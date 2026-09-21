using Microsoft.EntityFrameworkCore;
using GreenRetail.Core.Abstractions;
using GreenRetail.Data;

namespace GreenRetail.Features.Sync;

public interface ISyncOutboxService
{
    Task<int> GetPendingCountAsync(CancellationToken cancellationToken = default);
    Task<int> ProcessPendingAsync(CancellationToken cancellationToken = default);
}

public sealed class SyncOutboxService : ISyncOutboxService
{
    private readonly IDbContextFactory<PosDbContext> _dbContextFactory;
    private readonly IClock _clock;

    public SyncOutboxService(
        IDbContextFactory<PosDbContext> dbContextFactory,
        IClock clock)
    {
        _dbContextFactory = dbContextFactory;
        _clock = clock;
    }

    public async Task<int> GetPendingCountAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        return await db.SyncOutbox
            .AsNoTracking()
            .CountAsync(x => x.ProcessedUtc == null, cancellationToken);
    }

    public async Task<int> ProcessPendingAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        var pending = await db.SyncOutbox
            .Where(x => x.ProcessedUtc == null)
            .ToListAsync(cancellationToken);

        foreach (var item in pending)
        {
            // TODO:
            // Replace with real HTTP sync to central API.
            // Use idempotency keys, retries, circuit breaker, and signed payloads.
            item.ProcessedUtc = _clock.UtcNow;
        }

        await db.SaveChangesAsync(cancellationToken);

        return pending.Count;
    }
}