using Microsoft.EntityFrameworkCore;
using GreenRetail.Core.Abstractions;
using GreenRetail.Core.Results;
using GreenRetail.Data;
using GreenRetail.Data.Entities;

namespace GreenRetail.Procurement;

public sealed record CreateSupplierCommand(
    string Name,
    string? Phone,
    string? Email,
    string? Address);

public sealed record SupplierResult(
    Guid SupplierId,
    string Name);

public interface ICreateSupplierUseCase
    : IUseCase<CreateSupplierCommand, Result<SupplierResult>>
{
}

public sealed class CreateSupplierUseCase : ICreateSupplierUseCase
{
    private readonly IDbContextFactory<PosDbContext> _dbContextFactory;
    private readonly IClock _clock;

    public CreateSupplierUseCase(
        IDbContextFactory<PosDbContext> dbContextFactory,
        IClock clock)
    {
        _dbContextFactory = dbContextFactory;
        _clock = clock;
    }

    public async Task<Result<SupplierResult>> ExecuteAsync(
        CreateSupplierCommand command,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.Name))
        {
            return Result<SupplierResult>.Fail("Supplier name is required.");
        }

        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        var supplier = new Supplier
        {
            Name = command.Name.Trim(),
            Phone = command.Phone,
            Email = command.Email,
            Address = command.Address,
            IsActive = true,
            CreatedUtc = _clock.UtcNow
        };

        db.Suppliers.Add(supplier);
        await db.SaveChangesAsync(cancellationToken);

        return Result<SupplierResult>.Ok(new SupplierResult(supplier.Id, supplier.Name));
    }
}

public sealed record CreatePurchaseOrderLineInput(
    Guid ProductId,
    decimal OrderedQuantity,
    decimal UnitCost);

public sealed record CreatePurchaseOrderCommand(
    Guid SupplierId,
    string? Notes,
    Guid? CreatedByUserId,
    Guid? BranchId,
    IReadOnlyList<CreatePurchaseOrderLineInput> Lines);

public sealed record PurchaseOrderResult(
    Guid PurchaseOrderId,
    string Number,
    decimal TotalAmount);

public interface ICreatePurchaseOrderUseCase
    : IUseCase<CreatePurchaseOrderCommand, Result<PurchaseOrderResult>>
{
}

public sealed class CreatePurchaseOrderUseCase : ICreatePurchaseOrderUseCase
{
    private readonly IDbContextFactory<PosDbContext> _dbContextFactory;
    private readonly IClock _clock;

    public CreatePurchaseOrderUseCase(
        IDbContextFactory<PosDbContext> dbContextFactory,
        IClock clock)
    {
        _dbContextFactory = dbContextFactory;
        _clock = clock;
    }

    public async Task<Result<PurchaseOrderResult>> ExecuteAsync(
        CreatePurchaseOrderCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.Lines.Count == 0)
        {
            return Result<PurchaseOrderResult>.Fail("Purchase order must contain at least one line.");
        }

        if (command.Lines.Any(x => x.OrderedQuantity <= 0m))
        {
            return Result<PurchaseOrderResult>.Fail("Ordered quantity must be greater than zero.");
        }

        if (command.Lines.Any(x => x.UnitCost < 0m))
        {
            return Result<PurchaseOrderResult>.Fail("Unit cost cannot be negative.");
        }

        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        var supplierExists = await db.Suppliers
            .AnyAsync(x => x.Id == command.SupplierId && x.IsActive, cancellationToken);

        if (!supplierExists)
        {
            return Result<PurchaseOrderResult>.Fail("Supplier not found or inactive.");
        }

        var productIds = command.Lines
            .Select(x => x.ProductId)
            .Distinct()
            .ToList();

        var products = await db.Products
            .Where(x => productIds.Contains(x.Id))
            .ToListAsync(cancellationToken);

        if (products.Count != productIds.Count)
        {
            return Result<PurchaseOrderResult>.Fail("One or more products were not found.");
        }

        var number = $"PO-{DateTime.UtcNow:yyyyMMddHHmmssfff}";

        var purchaseOrder = new PurchaseOrder
        {
            Number = number,
            SupplierId = command.SupplierId,
            BranchId = command.BranchId,
            Notes = command.Notes,
            Status = PurchaseOrderStatus.Active,
            OrderDateUtc = _clock.UtcNow,
            CreatedByUserId = command.CreatedByUserId
        };

        foreach (var line in command.Lines)
        {
            purchaseOrder.Lines.Add(new PurchaseOrderLine
            {
                ProductId = line.ProductId,
                OrderedQuantity = line.OrderedQuantity,
                UnitCost = line.UnitCost
            });

            purchaseOrder.TotalAmount += line.OrderedQuantity * line.UnitCost;
        }

        db.PurchaseOrders.Add(purchaseOrder);
        await db.SaveChangesAsync(cancellationToken);

        return Result<PurchaseOrderResult>.Ok(new PurchaseOrderResult(
            purchaseOrder.Id,
            purchaseOrder.Number,
            purchaseOrder.TotalAmount));
    }
}

public sealed record PostGrnLineInput(
    Guid ProductId,
    decimal AcceptedQuantity,
    decimal RejectedQuantity,
    string? RejectReason,
    DateTime? ExpiryUtc,
    string? BatchNumber);

public sealed record PostGrnCommand(
    Guid? PurchaseOrderId,
    Guid SupplierId,
    string VendorInvoiceNumber,
    DateTime VendorInvoiceDate,
    string? Notes,
    Guid? PostedByUserId,
    Guid? BranchId,
    IReadOnlyList<PostGrnLineInput> Lines);

public sealed record GrnResult(
    Guid GoodsReceivedNoteId,
    string Number);

public interface IPostGrnUseCase
    : IUseCase<PostGrnCommand, Result<GrnResult>>
{
}

public sealed class PostGrnUseCase : IPostGrnUseCase
{
    private readonly IDbContextFactory<PosDbContext> _dbContextFactory;
    private readonly IClock _clock;

    public PostGrnUseCase(
        IDbContextFactory<PosDbContext> dbContextFactory,
        IClock clock)
    {
        _dbContextFactory = dbContextFactory;
        _clock = clock;
    }

    public async Task<Result<GrnResult>> ExecuteAsync(
        PostGrnCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.Lines.Count == 0)
        {
            return Result<GrnResult>.Fail("GRN must contain at least one line.");
        }

        if (string.IsNullOrWhiteSpace(command.VendorInvoiceNumber))
        {
            return Result<GrnResult>.Fail("Vendor invoice number is required.");
        }

        if (command.Lines.Any(x => x.AcceptedQuantity < 0m || x.RejectedQuantity < 0m))
        {
            return Result<GrnResult>.Fail("GRN quantities cannot be negative.");
        }

        if (command.Lines.Any(x => x.AcceptedQuantity == 0m && x.RejectedQuantity == 0m))
        {
            return Result<GrnResult>.Fail("Each GRN line must have accepted or rejected quantity.");
        }

        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        var supplierExists = await db.Suppliers
            .AnyAsync(x => x.Id == command.SupplierId && x.IsActive, cancellationToken);

        if (!supplierExists)
        {
            return Result<GrnResult>.Fail("Supplier not found or inactive.");
        }

        PurchaseOrder? purchaseOrder = null;

        if (command.PurchaseOrderId.HasValue)
        {
            purchaseOrder = await db.PurchaseOrders
                .Include(x => x.Lines)
                .FirstOrDefaultAsync(x => x.Id == command.PurchaseOrderId.Value, cancellationToken);

            if (purchaseOrder is null)
            {
                return Result<GrnResult>.Fail("Purchase order not found.");
            }

            if (purchaseOrder.Status != PurchaseOrderStatus.Active)
            {
                return Result<GrnResult>.Fail("Purchase order is not active.");
            }

            if (purchaseOrder.SupplierId != command.SupplierId)
            {
                return Result<GrnResult>.Fail("Supplier does not match the selected purchase order.");
            }
        }

        var productIds = command.Lines
            .Select(x => x.ProductId)
            .Distinct()
            .ToList();

        var products = await db.Products
            .Where(x => productIds.Contains(x.Id))
            .ToListAsync(cancellationToken);

        if (products.Count != productIds.Count)
        {
            return Result<GrnResult>.Fail("One or more products were not found.");
        }

        var productLookup = products.ToDictionary(x => x.Id);

        var grn = new GoodsReceivedNote
        {
            Number = $"GRN-{DateTime.UtcNow:yyyyMMddHHmmssfff}",
            PurchaseOrderId = command.PurchaseOrderId,
            SupplierId = command.SupplierId,
            VendorInvoiceNumber = command.VendorInvoiceNumber.Trim(),
            VendorInvoiceDate = command.VendorInvoiceDate,
            BranchId = command.BranchId,
            Status = GrnStatus.Posted,
            Notes = command.Notes,
            ReceivedUtc = _clock.UtcNow,
            PostedUtc = _clock.UtcNow,
            PostedByUserId = command.PostedByUserId
        };

        foreach (var line in command.Lines)
        {
            var product = productLookup[line.ProductId];

            var orderedQuantity = line.AcceptedQuantity + line.RejectedQuantity;

            if (purchaseOrder is not null)
            {
                var poLine = purchaseOrder.Lines
                    .FirstOrDefault(x => x.ProductId == line.ProductId);

                if (poLine is null)
                {
                    return Result<GrnResult>.Fail($"Product '{product.Name}' is not on the selected purchase order.");
                }

                orderedQuantity = poLine.OrderedQuantity;

                if (line.AcceptedQuantity + line.RejectedQuantity > poLine.OrderedQuantity)
                {
                    return Result<GrnResult>.Fail($"Received quantity cannot exceed ordered quantity for '{product.Name}'.");
                }
            }

            if (line.AcceptedQuantity > 0m)
            {
                if (!grn.BranchId.HasValue)
                    return Result<GrnResult>.Fail("A branch is required before received stock can be posted.");

                if (product.RequiresExpiry && line.ExpiryUtc is null)
                {
                    return Result<GrnResult>.Fail($"Expiry date is required for '{product.Name}'.");
                }

                if (product.RequiresExpiry && line.ExpiryUtc!.Value.Date < _clock.UtcNow.Date)
                {
                    return Result<GrnResult>.Fail($"Expired goods cannot be accepted for '{product.Name}'.");
                }

                if (product.RequiresBatch && string.IsNullOrWhiteSpace(line.BatchNumber))
                {
                    return Result<GrnResult>.Fail($"Batch number is required for '{product.Name}'.");
                }

                var stock = await db.StockLevels
                    .FirstOrDefaultAsync(x => x.ProductId == product.Id && x.BranchId == grn.BranchId.Value, cancellationToken);

                if (stock is null)
                {
                    stock = new StockLevel
                    {
                        ProductId = product.Id,
                        BranchId = grn.BranchId.Value,
                        Quantity = 0m
                    };

                    db.StockLevels.Add(stock);
                }

                stock.Quantity += line.AcceptedQuantity;

                db.StockLedger.Add(new StockLedgerEntry
                {
                    ProductId = product.Id,
                    BranchId = grn.BranchId.Value,
                    QuantityChange = line.AcceptedQuantity,
                    Reason = StockMovementReason.Purchase,
                    Note = $"GRN {grn.Number}",
                    CreatedUtc = _clock.UtcNow
                });
            }

            grn.Lines.Add(new GoodsReceivedNoteLine
            {
                ProductId = product.Id,
                OrderedQuantity = orderedQuantity,
                AcceptedQuantity = line.AcceptedQuantity,
                RejectedQuantity = line.RejectedQuantity,
                RejectReason = line.RejectReason,
                ExpiryUtc = line.ExpiryUtc,
                BatchNumber = line.BatchNumber
            });
        }

        if (purchaseOrder is not null)
        {
            purchaseOrder.Status = PurchaseOrderStatus.Closed;
            purchaseOrder.ClosedUtc = _clock.UtcNow;
        }

        db.GoodsReceivedNotes.Add(grn);

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return Result<GrnResult>.Ok(new GrnResult(grn.Id, grn.Number));
    }
}