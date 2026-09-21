using Microsoft.EntityFrameworkCore;
using GreenRetail.Core.Abstractions;
using GreenRetail.Core.Results;
using GreenRetail.Data;

namespace GreenRetail.Refunds;

public sealed record GetPendingRefundsQueryRequest(int Take = 20);

public sealed record RefundListItem(
    Guid Id,
    string SaleIdText,
    string Reason,
    string Total,
    string Status,
    string Created);

public interface IGetPendingRefundsQuery
    : IUseCase<GetPendingRefundsQueryRequest, Result<IReadOnlyList<RefundListItem>>>
{
}

public sealed class GetPendingRefundsQuery : IGetPendingRefundsQuery
{
    private readonly IDbContextFactory<PosDbContext> _dbContextFactory;

    public GetPendingRefundsQuery(IDbContextFactory<PosDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task<Result<IReadOnlyList<RefundListItem>>> ExecuteAsync(
        GetPendingRefundsQueryRequest request,
        CancellationToken cancellationToken = default)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        var items = await db.RefundRequests
            .AsNoTracking()
            .Where(x => x.Status == RefundStatus.Requested)
            .OrderByDescending(x => x.CreatedUtc)
            .Take(request.Take)
            .Select(x => new
            {
                x.Id,
                x.SaleId,
                x.Reason,
                x.TotalKobo,
                x.Status,
                x.CreatedUtc
            })
            .ToListAsync(cancellationToken);

        var result = items
            .Select(x => new RefundListItem(
                x.Id,
                x.SaleId.HasValue ? x.SaleId.Value.ToString() : string.Empty,
                x.Reason,
                FormatKobo(x.TotalKobo),
                x.Status.ToString(),
                x.CreatedUtc.ToString("yyyy-MM-dd HH:mm")))
            .ToList();

        return Result<IReadOnlyList<RefundListItem>>.Ok(result);
    }

    private static string FormatKobo(long kobo)
    {
        return $"₦{(kobo / 100m):N0}";
    }
}

public sealed record GetActiveVouchersQueryRequest(int Take = 20);

public sealed record VoucherListItem(
    Guid Id,
    string Code,
    string Amount,
    string Balance,
    string Status,
    string Issued);

public interface IGetActiveVouchersQuery
    : IUseCase<GetActiveVouchersQueryRequest, Result<IReadOnlyList<VoucherListItem>>>
{
}

public sealed class GetActiveVouchersQuery : IGetActiveVouchersQuery
{
    private readonly IDbContextFactory<PosDbContext> _dbContextFactory;

    public GetActiveVouchersQuery(IDbContextFactory<PosDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task<Result<IReadOnlyList<VoucherListItem>>> ExecuteAsync(
        GetActiveVouchersQueryRequest request,
        CancellationToken cancellationToken = default)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        var items = await db.StoreCreditVouchers
            .AsNoTracking()
            .Where(x => x.Status == StoreCreditVoucherStatus.Active)
            .OrderByDescending(x => x.IssuedUtc)
            .Take(request.Take)
            .Select(x => new
            {
                x.Id,
                x.Code,
                x.AmountKobo,
                x.BalanceKobo,
                x.Status,
                x.IssuedUtc
            })
            .ToListAsync(cancellationToken);

        var result = items
            .Select(x => new VoucherListItem(
                x.Id,
                x.Code,
                FormatKobo(x.AmountKobo),
                FormatKobo(x.BalanceKobo),
                x.Status.ToString(),
                x.IssuedUtc.ToString("yyyy-MM-dd HH:mm")))
            .ToList();

        return Result<IReadOnlyList<VoucherListItem>>.Ok(result);
    }

    private static string FormatKobo(long kobo)
    {
        return $"₦{(kobo / 100m):N0}";
    }
}