using Microsoft.EntityFrameworkCore;
using GreenRetail.Core.Abstractions;
using GreenRetail.Core.Results;
using GreenRetail.Data;
using GreenRetail.Core.ValueObjects;

namespace GreenRetail.SecurityOps;

public sealed record GetRecentSalesQueryRequest(int Take = 20);

public sealed record RecentSaleListItem(
    Guid Id,
    string Created,
    string Total,
    string CashierName,
    string ItemCount);

public interface IGetRecentSalesQuery
    : IUseCase<GetRecentSalesQueryRequest, Result<IReadOnlyList<RecentSaleListItem>>>
{
}

public sealed class GetRecentSalesQuery : IGetRecentSalesQuery
{
    private readonly IDbContextFactory<PosDbContext> _dbContextFactory;

    public GetRecentSalesQuery(IDbContextFactory<PosDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task<Result<IReadOnlyList<RecentSaleListItem>>> ExecuteAsync(
        GetRecentSalesQueryRequest request,
        CancellationToken cancellationToken = default)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        var items = await db.Sales
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedUtc)
            .Take(request.Take)
            .Select(x => new
            {
                x.Id,
                x.CreatedUtc,
                x.TotalKobo,
                x.CashierName,
                ItemCount = x.Items.Count
            })
            .ToListAsync(cancellationToken);

        var result = items
            .Select(x => new RecentSaleListItem(
                x.Id,
                x.CreatedUtc.ToString("yyyy-MM-dd HH:mm"),
                Money.FromNaira(x.TotalKobo / 100m).ToString(),
                x.CashierName,
                x.ItemCount.ToString()))
            .ToList();

        return Result<IReadOnlyList<RecentSaleListItem>>.Ok(result);
    }
}