using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
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
    private readonly ILogger<SyncOutboxService> _logger;

    public SyncOutboxService(IDbContextFactory<PosDbContext> dbContextFactory, ILogger<SyncOutboxService> logger)
    {
        _dbContextFactory = dbContextFactory;
        _logger = logger;
    }

    public async Task<int> GetPendingCountAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await db.SyncOutbox.CountAsync(x => x.ProcessedUtc == null, cancellationToken);
    }

    public async Task<int> ProcessPendingAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var pending = await db.SyncOutbox.Where(x => x.ProcessedUtc == null).OrderBy(x => x.CreatedUtc).Take(100).ToListAsync(cancellationToken);
        if (pending.Count == 0) return 0;

        foreach (var item in pending)
        {
            item.RetryCount++;
            item.LastError = "Synchronization transport is not configured; item remains pending.";
        }
        await db.SaveChangesAsync(cancellationToken);
        _logger.LogWarning("{Count} sync outbox item(s) remain pending because no sync transport is configured.", pending.Count);
        return 0;
    }
}
