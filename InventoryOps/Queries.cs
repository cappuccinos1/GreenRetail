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
public sealed record GetReadyReceivingQueryRequest(Guid UserId, int Take = 30);

public sealed record ReadyReceivingListItem(
    Guid Id,
    string Number,
    string SupplierName,
    string BranchName,
    string VendorInvoiceNumber,
    string AcceptedSummary,
    DateTime ReceivedUtc);

public interface IGetReadyReceivingQuery
    : IUseCase<GetReadyReceivingQueryRequest, Result<IReadOnlyList<ReadyReceivingListItem>>>;

public sealed class GetReadyReceivingQuery : IGetReadyReceivingQuery
{
    private readonly IDbContextFactory<PosDbContext> _dbFactory;
    private readonly GreenRetail.Rbac.IAuthorizationService _authorization;

    public GetReadyReceivingQuery(IDbContextFactory<PosDbContext> dbFactory, GreenRetail.Rbac.IAuthorizationService authorization)
    {
        _dbFactory = dbFactory;
        _authorization = authorization;
    }

    public async Task<Result<IReadOnlyList<ReadyReceivingListItem>>> ExecuteAsync(
        GetReadyReceivingQueryRequest request,
        CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var sessions = await db.ReceivingSessions
            .AsNoTracking()
            .Include(x => x.Supplier)
            .Include(x => x.Branch)
            .Include(x => x.Lines)
            .Where(x => x.Status == GreenRetail.Procurement.ReceivingStatus.ReadyForPosting)
            .OrderByDescending(x => x.ReceivedUtc)
            .Take(request.Take)
            .ToListAsync(cancellationToken);

        var allowed = new List<ReadyReceivingListItem>();
        foreach (var x in sessions)
        {
            if (!await _authorization.HasPermissionAsync(
                    request.UserId,
                    GreenRetail.Rbac.PermissionCodes.ReceivingGrnPost,
                    x.BranchId,
                    cancellationToken))
                continue;

            allowed.Add(new ReadyReceivingListItem(
                x.Id,
                x.Number,
                x.Supplier?.Name ?? "Unknown supplier",
                x.Branch?.Name ?? "Unknown branch",
                x.VendorInvoiceNumber,
                $"{x.Lines.Sum(l => l.AcceptedQuantity):0.###} accepted / {x.Lines.Sum(l => l.RejectedQuantity):0.###} rejected",
                x.ReceivedUtc));
        }

        return Result<IReadOnlyList<ReadyReceivingListItem>>.Ok(allowed);
    }
}
