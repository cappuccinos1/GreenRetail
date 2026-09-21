using Microsoft.EntityFrameworkCore;
using GreenRetail.Core.Abstractions;
using GreenRetail.Core.Results;
using GreenRetail.Data;

namespace GreenRetail.Procurement;

public sealed record GetSuppliersQueryRequest(string? Search, int Take = 50);

public sealed record SupplierListItem(
    Guid Id,
    string Name,
    string? Phone,
    bool IsActive);

public interface IGetSuppliersQuery
    : IUseCase<GetSuppliersQueryRequest, Result<IReadOnlyList<SupplierListItem>>>
{
}

public sealed class GetSuppliersQuery : IGetSuppliersQuery
{
    private readonly IDbContextFactory<PosDbContext> _dbContextFactory;

    public GetSuppliersQuery(IDbContextFactory<PosDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task<Result<IReadOnlyList<SupplierListItem>>> ExecuteAsync(
        GetSuppliersQueryRequest request,
        CancellationToken cancellationToken = default)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        IQueryable<Supplier> query = db.Suppliers.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();

            query = query.Where(x =>
                x.Name.Contains(term) ||
                (x.Phone != null && x.Phone.Contains(term)));
        }

        var suppliers = await query
            .OrderBy(x => x.Name)
            .Take(request.Take)
            .Select(x => new SupplierListItem(
                x.Id,
                x.Name,
                x.Phone,
                x.IsActive))
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<SupplierListItem>>.Ok(suppliers);
    }
}

public sealed record GetActivePurchaseOrdersQueryRequest(int Take = 50);

public sealed record PurchaseOrderListItem(
    Guid Id,
    string Number,
    Guid SupplierId,
    string SupplierName,
    string Status,
    string Total,
    string Created);

public interface IGetActivePurchaseOrdersQuery
    : IUseCase<GetActivePurchaseOrdersQueryRequest, Result<IReadOnlyList<PurchaseOrderListItem>>>
{
}

public sealed class GetActivePurchaseOrdersQuery : IGetActivePurchaseOrdersQuery
{
    private readonly IDbContextFactory<PosDbContext> _dbContextFactory;

    public GetActivePurchaseOrdersQuery(IDbContextFactory<PosDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task<Result<IReadOnlyList<PurchaseOrderListItem>>> ExecuteAsync(
        GetActivePurchaseOrdersQueryRequest request,
        CancellationToken cancellationToken = default)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        var purchaseOrders = await db.PurchaseOrders
            .AsNoTracking()
            .Where(x => x.Status == PurchaseOrderStatus.Active)
            .OrderByDescending(x => x.OrderDateUtc)
            .Take(request.Take)
            .Select(x => new
            {
                x.Id,
                x.Number,
                x.SupplierId,
                SupplierName = x.Supplier != null ? x.Supplier.Name : string.Empty,
                x.Status,
                x.TotalAmount,
                x.OrderDateUtc
            })
            .ToListAsync(cancellationToken);

        var result = purchaseOrders
            .Select(x => new PurchaseOrderListItem(
                x.Id,
                x.Number,
                x.SupplierId,
                x.SupplierName,
                x.Status.ToString(),
                $"₦{x.TotalAmount:N0}",
                x.OrderDateUtc.ToString("yyyy-MM-dd HH:mm")))
            .ToList();

        return Result<IReadOnlyList<PurchaseOrderListItem>>.Ok(result);
    }
}