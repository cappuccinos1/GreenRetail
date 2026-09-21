using Microsoft.EntityFrameworkCore;
using GreenRetail.Core.Abstractions;
using GreenRetail.Core.Results;
using GreenRetail.Core.ValueObjects;
using GreenRetail.Data;
using GreenRetail.Data.Entities;

namespace GreenRetail.Features.Reports;

public sealed record DailyReportReadModel(
    DateTime GeneratedUtc,
    int SalesCount,
    Money TotalSales,
    Money Cash,
    Money BankTransfer,
    Money Card,
    Money MobileMoney,
    Money EstimatedProfit,
    int LowStockCount);

public interface IGetDailyReportQuery
    : IUseCase<EmptyRequest, Result<DailyReportReadModel>>
{
}

public sealed class GetDailyReportQuery : IGetDailyReportQuery
{
    private readonly IDbContextFactory<PosDbContext> _dbContextFactory;
    private readonly IClock _clock;

    public GetDailyReportQuery(
        IDbContextFactory<PosDbContext> dbContextFactory,
        IClock clock)
    {
        _dbContextFactory = dbContextFactory;
        _clock = clock;
    }

    public async Task<Result<DailyReportReadModel>> ExecuteAsync(
        EmptyRequest request,
        CancellationToken cancellationToken = default)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        var start = _clock.UtcNow.Date;
        var end = start.AddDays(1);

        var sales = await db.Sales
            .AsNoTracking()
            .Where(x => x.CreatedUtc >= start && x.CreatedUtc < end)
            .Select(x => new { x.TotalKobo })
            .ToListAsync(cancellationToken);

        var payments = await db.Payments
            .AsNoTracking()
            .Where(x => x.Sale != null && x.Sale.CreatedUtc >= start && x.Sale.CreatedUtc < end)
            .Select(x => new { x.Method, x.AmountKobo })
            .ToListAsync(cancellationToken);

        var saleItems = await db.SaleItems
            .AsNoTracking()
            .Where(x => x.Sale != null && x.Sale.CreatedUtc >= start && x.Sale.CreatedUtc < end)
            .Select(x => new
            {
                x.TotalKobo,
                CostNaira = x.Product != null ? x.Product.CostPrice * x.Quantity : 0m
            })
            .ToListAsync(cancellationToken);

        var lowStockCount = await db.StockLevels
            .AsNoTracking()
            .CountAsync(x => x.Quantity <= 5m, cancellationToken);

        var totalSalesKobo = sales.Sum(x => x.TotalKobo);

        var cash = payments
            .Where(x => x.Method == PaymentMethod.Cash)
            .Sum(x => x.AmountKobo);

        var bankTransfer = payments
            .Where(x => x.Method == PaymentMethod.BankTransfer)
            .Sum(x => x.AmountKobo);

        var card = payments
            .Where(x => x.Method == PaymentMethod.Card || x.Method == PaymentMethod.PosTerminal)
            .Sum(x => x.AmountKobo);

        var mobileMoney = payments
            .Where(x => x.Method == PaymentMethod.MobileMoney)
            .Sum(x => x.AmountKobo);

        var estimatedProfitKobo = saleItems.Sum(x => x.TotalKobo - Money.FromNaira(x.CostNaira).Kobo);

        return Result<DailyReportReadModel>.Ok(new DailyReportReadModel(
            _clock.UtcNow,
            sales.Count,
            Money.FromNaira(totalSalesKobo / 100m),
            Money.FromNaira(cash / 100m),
            Money.FromNaira(bankTransfer / 100m),
            Money.FromNaira(card / 100m),
            Money.FromNaira(mobileMoney / 100m),
            Money.FromNaira(estimatedProfitKobo / 100m),
            lowStockCount));
    }
}