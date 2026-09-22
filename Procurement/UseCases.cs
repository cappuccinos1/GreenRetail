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
    public Task<Result<GrnResult>> ExecuteAsync(
        PostGrnCommand command,
        CancellationToken cancellationToken = default)
    {
        // Source-compatible guard for the old UI contract. Direct GRN posting is
        // intentionally disabled so Quality Control cannot bypass the receiving staging boundary.
        return Task.FromResult(Result<GrnResult>.Fail(
            "Direct GRN posting is disabled. Goods must pass through receiving and Quality Control staging before Inventory posts the GRN.",
            ResultErrorCode.BusinessRule));
    }
}
