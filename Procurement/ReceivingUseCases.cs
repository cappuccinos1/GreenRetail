using Microsoft.EntityFrameworkCore;
using GreenRetail.Core.Abstractions;
using GreenRetail.Core.Results;
using GreenRetail.Data;
using GreenRetail.Rbac;

namespace GreenRetail.Procurement;

public sealed record StartReceivingLineInput(
    Guid ProductId,
    decimal DeliveredQuantity,
    long UnitCostKobo);

public sealed record StartReceivingCommand(
    Guid? PurchaseOrderId,
    Guid SupplierId,
    Guid BranchId,
    string VendorInvoiceNumber,
    DateTime VendorInvoiceDate,
    string? Notes,
    Guid ReceivedByUserId,
    IReadOnlyList<StartReceivingLineInput> Lines);

public sealed record ReceivingResult(Guid ReceivingSessionId, string Number);

public interface IStartReceivingUseCase
    : IUseCase<StartReceivingCommand, Result<ReceivingResult>>;

public sealed class StartReceivingUseCase : IStartReceivingUseCase
{
    private readonly IDbContextFactory<PosDbContext> _dbContextFactory;
    private readonly IAuthorizationService _authorization;
    private readonly IClock _clock;

    public StartReceivingUseCase(
        IDbContextFactory<PosDbContext> dbContextFactory,
        IAuthorizationService authorization,
        IClock clock)
    {
        _dbContextFactory = dbContextFactory;
        _authorization = authorization;
        _clock = clock;
    }

    public async Task<Result<ReceivingResult>> ExecuteAsync(
        StartReceivingCommand command,
        CancellationToken cancellationToken = default)
    {
        if (!await _authorization.HasPermissionAsync(
                command.ReceivedByUserId,
                PermissionCodes.QualityControlInspect,
                command.BranchId,
                cancellationToken))
            return Result<ReceivingResult>.Fail("You are not authorized to start receiving / Quality Control.", ResultErrorCode.Authorization);

        if (!command.PurchaseOrderId.HasValue && !await _authorization.HasPermissionAsync(
                command.ReceivedByUserId,
                PermissionCodes.ReceivingInvoiceWithoutPo,
                command.BranchId,
                cancellationToken))
            return Result<ReceivingResult>.Fail("No-PO receiving requires the no-PO receiving permission.", ResultErrorCode.Authorization);

        if (command.Lines.Count == 0)
            return Result<ReceivingResult>.Fail("Receiving must contain at least one product line.");

        if (string.IsNullOrWhiteSpace(command.VendorInvoiceNumber))
            return Result<ReceivingResult>.Fail("Vendor invoice number is required.");

        if (command.BranchId == Guid.Empty)
            return Result<ReceivingResult>.Fail("Branch is required.");

        if (command.Lines.Any(x => x.ProductId == Guid.Empty || x.DeliveredQuantity <= 0m || x.UnitCostKobo < 0))
            return Result<ReceivingResult>.Fail("Every receiving line must have a valid product, quantity and non-negative cost.");

        var duplicateProduct = command.Lines
            .GroupBy(x => x.ProductId)
            .FirstOrDefault(g => g.Count() > 1);
        if (duplicateProduct is not null)
            return Result<ReceivingResult>.Fail("A product may appear only once in a receiving transaction.");

        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        var branchExists = await db.Branches.AnyAsync(x => x.Id == command.BranchId && x.IsActive, cancellationToken);
        if (!branchExists)
            return Result<ReceivingResult>.Fail("Branch not found or inactive.", ResultErrorCode.NotFound);

        var supplierExists = await db.Suppliers.AnyAsync(x => x.Id == command.SupplierId && x.IsActive, cancellationToken);
        if (!supplierExists)
            return Result<ReceivingResult>.Fail("Supplier not found or inactive.", ResultErrorCode.NotFound);

        PurchaseOrder? purchaseOrder = null;
        if (command.PurchaseOrderId.HasValue)
        {
            purchaseOrder = await db.PurchaseOrders
                .Include(x => x.Lines)
                .FirstOrDefaultAsync(x => x.Id == command.PurchaseOrderId.Value, cancellationToken);

            if (purchaseOrder is null)
                return Result<ReceivingResult>.Fail("Purchase order not found.", ResultErrorCode.NotFound);

            if (purchaseOrder.Status != PurchaseOrderStatus.Active)
                return Result<ReceivingResult>.Fail("Purchase order is not active.");

            if (purchaseOrder.SupplierId != command.SupplierId)
                return Result<ReceivingResult>.Fail("Supplier does not match the purchase order.");

            if (purchaseOrder.BranchId.HasValue && purchaseOrder.BranchId.Value != command.BranchId)
                return Result<ReceivingResult>.Fail("Receiving branch does not match the purchase order branch.");

            var poProducts = purchaseOrder.Lines.Select(x => x.ProductId).ToHashSet();
            if (command.Lines.Any(x => !poProducts.Contains(x.ProductId)))
                return Result<ReceivingResult>.Fail("A received product is not on the selected purchase order.");

            foreach (var line in command.Lines)
            {
                var poLine = purchaseOrder.Lines.First(x => x.ProductId == line.ProductId);
                if (line.DeliveredQuantity > poLine.OrderedQuantity)
                    return Result<ReceivingResult>.Fail("Delivered quantity cannot exceed the purchase order quantity.");
            }
        }

        var productIds = command.Lines.Select(x => x.ProductId).ToList();
        var products = await db.Products.Where(x => productIds.Contains(x.Id) && x.IsActive).ToListAsync(cancellationToken);
        if (products.Count != productIds.Count)
            return Result<ReceivingResult>.Fail("One or more products were not found or are inactive.", ResultErrorCode.NotFound);

        var now = _clock.UtcNow;
        var receiving = new ReceivingSession
        {
            Number = $"RCV-{now:yyyyMMddHHmmssfff}",
            PurchaseOrderId = command.PurchaseOrderId,
            SupplierId = command.SupplierId,
            BranchId = command.BranchId,
            VendorInvoiceNumber = command.VendorInvoiceNumber.Trim(),
            VendorInvoiceDate = command.VendorInvoiceDate,
            Notes = command.Notes,
            Status = ReceivingStatus.InInspection,
            ReceivedUtc = now,
            ReceivedByUserId = command.ReceivedByUserId
        };

        foreach (var line in command.Lines)
        {
            receiving.Lines.Add(new ReceivingLine
            {
                ProductId = line.ProductId,
                DeliveredQuantity = line.DeliveredQuantity,
                UnitCostKobo = line.UnitCostKobo
            });
        }

        db.ReceivingSessions.Add(receiving);
        db.AuditLog.Add(new Data.Entities.AuditLogEntry
        {
            UserId = command.ReceivedByUserId,
            CreatedUtc = now,
            Action = "receiving.started",
            Details = $"Receiving {receiving.Number} started for supplier {command.SupplierId}; PO {command.PurchaseOrderId?.ToString() ?? "none"}."
        });

        await db.SaveChangesAsync(cancellationToken);
        return Result<ReceivingResult>.Ok(new ReceivingResult(receiving.Id, receiving.Number));
    }
}

public sealed record CompleteQualityControlLineInput(
    Guid ReceivingLineId,
    decimal AcceptedQuantity,
    decimal RejectedQuantity,
    string? RejectReason,
    DateTime ExpiryUtc,
    string? BatchNumber);

public sealed record CompleteQualityControlCommand(
    Guid ReceivingSessionId,
    Guid InspectorUserId,
    IReadOnlyList<CompleteQualityControlLineInput> Lines);

public interface ICompleteQualityControlUseCase
    : IUseCase<CompleteQualityControlCommand, Result<ReceivingResult>>;

public sealed class CompleteQualityControlUseCase : ICompleteQualityControlUseCase
{
    private readonly IDbContextFactory<PosDbContext> _dbContextFactory;
    private readonly IAuthorizationService _authorization;
    private readonly IClock _clock;

    public CompleteQualityControlUseCase(
        IDbContextFactory<PosDbContext> dbContextFactory,
        IAuthorizationService authorization,
        IClock clock)
    {
        _dbContextFactory = dbContextFactory;
        _authorization = authorization;
        _clock = clock;
    }

    public async Task<Result<ReceivingResult>> ExecuteAsync(
        CompleteQualityControlCommand command,
        CancellationToken cancellationToken = default)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var receiving = await db.ReceivingSessions
            .Include(x => x.Lines)
            .Include(x => x.PurchaseOrder)
                .ThenInclude(x => x!.Lines)
            .Include(x => x.Lines)
                .ThenInclude(x => x.Product)
            .FirstOrDefaultAsync(x => x.Id == command.ReceivingSessionId, cancellationToken);

        if (receiving is null)
            return Result<ReceivingResult>.Fail("Receiving session not found.", ResultErrorCode.NotFound);

        if (!await _authorization.HasPermissionAsync(
                command.InspectorUserId,
                PermissionCodes.QualityControlInspect,
                receiving.BranchId,
                cancellationToken))
            return Result<ReceivingResult>.Fail("You are not authorized to perform Quality Control.", ResultErrorCode.Authorization);

        if (receiving.Status != ReceivingStatus.InInspection)
            return Result<ReceivingResult>.Fail("This receiving session is no longer awaiting Quality Control.");

        if (command.Lines.GroupBy(x => x.ReceivingLineId).Any(g => g.Count() > 1))
            return Result<ReceivingResult>.Fail("A receiving line may be inspected only once in a Quality Control submission.");

        var inputById = command.Lines.ToDictionary(x => x.ReceivingLineId);
        if (inputById.Count != receiving.Lines.Count || receiving.Lines.Any(x => !inputById.ContainsKey(x.Id)))
            return Result<ReceivingResult>.Fail("Quality Control must inspect every receiving line.");

        foreach (var line in receiving.Lines)
        {
            var input = inputById[line.Id];
            if (input.AcceptedQuantity < 0m || input.RejectedQuantity < 0m)
                return Result<ReceivingResult>.Fail("Quality Control quantities cannot be negative.");

            if (input.AcceptedQuantity + input.RejectedQuantity != line.DeliveredQuantity)
                return Result<ReceivingResult>.Fail("Accepted plus rejected quantity must equal delivered quantity.");

            if (input.RejectedQuantity > 0m && string.IsNullOrWhiteSpace(input.RejectReason))
                return Result<ReceivingResult>.Fail($"A rejection reason is required for '{line.Product?.Name ?? "the product"}'.");

            // Expiry is mandatory for every incoming product line.
            if (input.ExpiryUtc == default)
                return Result<ReceivingResult>.Fail($"Expiry date is required for '{line.Product?.Name ?? "the product"}'.");

            if (input.ExpiryUtc.Date < _clock.UtcNow.Date)
                return Result<ReceivingResult>.Fail($"Expired goods cannot be accepted for '{line.Product?.Name ?? "the product"}'.");

            if (line.Product?.RequiresBatch == true && string.IsNullOrWhiteSpace(input.BatchNumber))
                return Result<ReceivingResult>.Fail($"Batch number is required for '{line.Product.Name}'.");

            line.AcceptedQuantity = input.AcceptedQuantity;
            line.RejectedQuantity = input.RejectedQuantity;
            line.RejectReason = string.IsNullOrWhiteSpace(input.RejectReason) ? null : input.RejectReason.Trim();
            line.ExpiryUtc = input.ExpiryUtc;
            line.BatchNumber = string.IsNullOrWhiteSpace(input.BatchNumber) ? null : input.BatchNumber.Trim();
        }

        var now = _clock.UtcNow;
        receiving.Status = ReceivingStatus.ReadyForPosting;
        receiving.InspectionCompletedUtc = now;
        receiving.InspectedByUserId = command.InspectorUserId;

        db.AuditLog.Add(new Data.Entities.AuditLogEntry
        {
            UserId = command.InspectorUserId,
            CreatedUtc = now,
            Action = "receiving.quality_control.completed",
            Details = $"Receiving {receiving.Number} inspected. Accepted={receiving.Lines.Sum(x => x.AcceptedQuantity):0.###}; rejected={receiving.Lines.Sum(x => x.RejectedQuantity):0.###}."
        });

        await db.SaveChangesAsync(cancellationToken);
        return Result<ReceivingResult>.Ok(new ReceivingResult(receiving.Id, receiving.Number));
    }
}

public sealed record ConfirmNoPoReceivingCommand(Guid ReceivingSessionId, Guid BuyerUserId);

public interface IConfirmNoPoReceivingUseCase
    : IUseCase<ConfirmNoPoReceivingCommand, Result<ReceivingResult>>;

public sealed class ConfirmNoPoReceivingUseCase : IConfirmNoPoReceivingUseCase
{
    private readonly IDbContextFactory<PosDbContext> _dbContextFactory;
    private readonly IAuthorizationService _authorization;
    private readonly IClock _clock;

    public ConfirmNoPoReceivingUseCase(
        IDbContextFactory<PosDbContext> dbContextFactory,
        IAuthorizationService authorization,
        IClock clock)
    {
        _dbContextFactory = dbContextFactory;
        _authorization = authorization;
        _clock = clock;
    }

    public async Task<Result<ReceivingResult>> ExecuteAsync(
        ConfirmNoPoReceivingCommand command,
        CancellationToken cancellationToken = default)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var receiving = await db.ReceivingSessions.FirstOrDefaultAsync(x => x.Id == command.ReceivingSessionId, cancellationToken);
        if (receiving is null)
            return Result<ReceivingResult>.Fail("Receiving session not found.", ResultErrorCode.NotFound);

        if (receiving.PurchaseOrderId.HasValue)
            return Result<ReceivingResult>.Fail("This receiving session already has a purchase order.");

        if (!await _authorization.HasPermissionAsync(
                command.BuyerUserId,
                PermissionCodes.ReceivingNoPoConfirm,
                receiving.BranchId,
                cancellationToken))
            return Result<ReceivingResult>.Fail("A buying officer must confirm a no-PO receiving transaction.", ResultErrorCode.Authorization);

        if (receiving.Status is ReceivingStatus.Posted or ReceivingStatus.Cancelled)
            return Result<ReceivingResult>.Fail("This receiving session can no longer be confirmed.");

        var now = _clock.UtcNow;
        receiving.NoPoBuyerConfirmedUtc = now;
        receiving.NoPoBuyerConfirmedByUserId = command.BuyerUserId;

        db.AuditLog.Add(new Data.Entities.AuditLogEntry
        {
            UserId = command.BuyerUserId,
            CreatedUtc = now,
            Action = "receiving.no_po.confirmed",
            Details = $"No-PO receiving {receiving.Number} confirmed by buying officer."
        });

        await db.SaveChangesAsync(cancellationToken);
        return Result<ReceivingResult>.Ok(new ReceivingResult(receiving.Id, receiving.Number));
    }
}

public sealed record PostReceivingGrnCommand(Guid ReceivingSessionId, Guid InventoryUserId);

public interface IPostReceivingGrnUseCase
    : IUseCase<PostReceivingGrnCommand, Result<GrnResult>>;

public sealed class PostReceivingGrnUseCase : IPostReceivingGrnUseCase
{
    private readonly IDbContextFactory<PosDbContext> _dbContextFactory;
    private readonly IAuthorizationService _authorization;
    private readonly IClock _clock;

    public PostReceivingGrnUseCase(
        IDbContextFactory<PosDbContext> dbContextFactory,
        IAuthorizationService authorization,
        IClock clock)
    {
        _dbContextFactory = dbContextFactory;
        _authorization = authorization;
        _clock = clock;
    }

    public async Task<Result<GrnResult>> ExecuteAsync(
        PostReceivingGrnCommand command,
        CancellationToken cancellationToken = default)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        var receiving = await db.ReceivingSessions
            .Include(x => x.Lines)
            .Include(x => x.PurchaseOrder)
            .FirstOrDefaultAsync(x => x.Id == command.ReceivingSessionId, cancellationToken);

        if (receiving is null)
            return Result<GrnResult>.Fail("Receiving session not found.", ResultErrorCode.NotFound);

        if (!await _authorization.HasPermissionAsync(
                command.InventoryUserId,
                PermissionCodes.ReceivingGrnPost,
                receiving.BranchId,
                cancellationToken))
            return Result<GrnResult>.Fail("You are not authorized to post received stock.", ResultErrorCode.Authorization);

        if (receiving.Status != ReceivingStatus.ReadyForPosting)
            return Result<GrnResult>.Fail("Only Quality Control-completed receiving sessions can be posted.");

        if (!receiving.PurchaseOrderId.HasValue && receiving.NoPoBuyerConfirmedByUserId is null)
            return Result<GrnResult>.Fail("A no-PO receiving transaction must be confirmed by a buying officer before posting.");

        var now = _clock.UtcNow;
        var grn = new GoodsReceivedNote
        {
            Number = $"GRN-{now:yyyyMMddHHmmssfff}",
            PurchaseOrderId = receiving.PurchaseOrderId,
            SupplierId = receiving.SupplierId,
            VendorInvoiceNumber = receiving.VendorInvoiceNumber,
            VendorInvoiceDate = receiving.VendorInvoiceDate,
            BranchId = receiving.BranchId,
            Status = GrnStatus.Posted,
            Notes = receiving.Notes,
            ReceivedUtc = receiving.ReceivedUtc,
            PostedUtc = now,
            PostedByUserId = command.InventoryUserId
        };

        foreach (var line in receiving.Lines)
        {
            if (line.AcceptedQuantity > 0m)
            {
                var stock = await db.StockLevels
                    .FirstOrDefaultAsync(x => x.ProductId == line.ProductId && x.BranchId == receiving.BranchId, cancellationToken);

                if (stock is null)
                {
                    stock = new Data.Entities.StockLevel
                    {
                        ProductId = line.ProductId,
                        BranchId = receiving.BranchId,
                        Quantity = 0m
                    };
                    db.StockLevels.Add(stock);
                }

                stock.Quantity += line.AcceptedQuantity;
                db.StockLedger.Add(new Data.Entities.StockLedgerEntry
                {
                    ProductId = line.ProductId,
                    BranchId = receiving.BranchId,
                    QuantityChange = line.AcceptedQuantity,
                    Reason = Data.Entities.StockMovementReason.Purchase,
                    Note = $"GRN {grn.Number} from receiving {receiving.Number}",
                    CreatedUtc = now
                });
            }

            grn.Lines.Add(new GoodsReceivedNoteLine
            {
                ProductId = line.ProductId,
                OrderedQuantity = receiving.PurchaseOrder?.Lines.FirstOrDefault(x => x.ProductId == line.ProductId)?.OrderedQuantity ?? line.DeliveredQuantity,
                AcceptedQuantity = line.AcceptedQuantity,
                RejectedQuantity = line.RejectedQuantity,
                RejectReason = line.RejectReason,
                ExpiryUtc = line.ExpiryUtc,
                BatchNumber = line.BatchNumber
            });
        }

        if (receiving.PurchaseOrder is not null)
        {
            receiving.PurchaseOrder.Status = PurchaseOrderStatus.Closed;
            receiving.PurchaseOrder.ClosedUtc = now;
        }

        receiving.Status = ReceivingStatus.Posted;
        receiving.PostedUtc = now;
        receiving.PostedByUserId = command.InventoryUserId;

        db.GoodsReceivedNotes.Add(grn);
        db.AuditLog.Add(new Data.Entities.AuditLogEntry
        {
            UserId = command.InventoryUserId,
            CreatedUtc = now,
            Action = "receiving.grn.posted",
            Details = $"GRN {grn.Number} posted from receiving {receiving.Number}. Accepted={receiving.Lines.Sum(x => x.AcceptedQuantity):0.###}; rejected={receiving.Lines.Sum(x => x.RejectedQuantity):0.###}."
        });

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return Result<GrnResult>.Ok(new GrnResult(grn.Id, grn.Number));
    }
}
