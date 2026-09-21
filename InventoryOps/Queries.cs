using Microsoft.EntityFrameworkCore;
using GreenRetail.Core.Abstractions;
using GreenRetail.Core.Results;
using GreenRetail.Data;

namespace GreenRetail.InventoryOps;

public sealed record GetPendingStockAdjustmentsQueryRequest(int Take = 20);

public sealed record StockAdjustmentListItem(
    Guid Id,
    string ProductName,
    string QuantityChange,
    string Reason,
    string Status,
    string Created);

public interface IGetPendingStockAdjustmentsQuery
    : IUseCase<GetPendingStockAdjustmentsQueryRequest, Result<IReadOnlyList<StockAdjustmentListItem>>>
{
}

public sealed class GetPendingStockAdjustmentsQuery : IGetPendingStockAdjustmentsQuery
{
    private readonly IDbContextFactory<PosDbContext> _dbContextFactory;

    public GetPendingStockAdjustmentsQuery(IDbContextFactory<PosDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task<Result<IReadOnlyList<StockAdjustmentListItem>>> ExecuteAsync(
        GetPendingStockAdjustmentsQueryRequest request,
        CancellationToken cancellationToken = default)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        var items = await db.StockAdjustmentRequests
            .AsNoTracking()
            .Where(x => x.Status == StockAdjustmentStatus.Requested)
            .OrderByDescending(x => x.CreatedUtc)
            .Take(request.Take)
            .Select(x => new
            {
                x.Id,
                ProductName = x.Product != null ? x.Product.Name : string.Empty,
                x.QuantityChange,
                x.Reason,
                x.Status,
                x.CreatedUtc
            })
            .ToListAsync(cancellationToken);

        var result = items
            .Select(x => new StockAdjustmentListItem(
                x.Id,
                x.ProductName,
                x.QuantityChange.ToString("0.###"),
                x.Reason,
                x.Status.ToString(),
                x.CreatedUtc.ToString("yyyy-MM-dd HH:mm")))
            .ToList();

        return Result<IReadOnlyList<StockAdjustmentListItem>>.Ok(result);
    }
}

public sealed record GetPendingStockOverridesQueryRequest(int Take = 20);

public sealed record StockOverrideListItem(
    Guid Id,
    string ProductName,
    string Reason,
    string Status,
    string Created);

public interface IGetPendingStockOverridesQuery
    : IUseCase<GetPendingStockOverridesQueryRequest, Result<IReadOnlyList<StockOverrideListItem>>>
{
}

public sealed class GetPendingStockOverridesQuery : IGetPendingStockOverridesQuery
{
    private readonly IDbContextFactory<PosDbContext> _dbContextFactory;

    public GetPendingStockOverridesQuery(IDbContextFactory<PosDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task<Result<IReadOnlyList<StockOverrideListItem>>> ExecuteAsync(
        GetPendingStockOverridesQueryRequest request,
        CancellationToken cancellationToken = default)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        var items = await db.StockOverrideRequests
            .AsNoTracking()
            .Where(x => x.Status == StockOverrideStatus.Requested)
            .OrderByDescending(x => x.CreatedUtc)
            .Take(request.Take)
            .Select(x => new
            {
                x.Id,
                ProductName = x.Product != null ? x.Product.Name : string.Empty,
                x.Reason,
                x.Status,
                x.CreatedUtc
            })
            .ToListAsync(cancellationToken);

        var result = items
            .Select(x => new StockOverrideListItem(
                x.Id,
                x.ProductName,
                x.Reason ?? string.Empty,
                x.Status.ToString(),
                x.CreatedUtc.ToString("yyyy-MM-dd HH:mm")))
            .ToList();

        return Result<IReadOnlyList<StockOverrideListItem>>.Ok(result);
    }
}