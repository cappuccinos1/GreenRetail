using Microsoft.EntityFrameworkCore;
using GreenRetail.Core.Abstractions;
using GreenRetail.Core.Results;
using GreenRetail.Core.ValueObjects;
using GreenRetail.Data;
using GreenRetail.Data.Entities;

namespace GreenRetail.Features.CashSessions;

public sealed record CashSessionReadModel(
    Guid Id,
    string CashierName,
    bool IsOpen,
    Money OpeningCash,
    Money ExpectedCash,
    Money? CountedCash,
    Money? Variance);

public sealed record OpenCashSessionCommand(
    Money OpeningCash,
    Guid? CashierId,
    string CashierName);

public sealed record CloseCashSessionCommand(
    Money CountedCash,
    Guid? UserId,
    string UserName);

public interface IOpenCashSessionUseCase
    : IUseCase<OpenCashSessionCommand, Result<CashSessionReadModel>>
{
}

public interface ICloseCashSessionUseCase
    : IUseCase<CloseCashSessionCommand, Result<CashSessionReadModel>>
{
}

public interface IGetOpenCashSessionQuery
    : IUseCase<EmptyRequest, Result<CashSessionReadModel?>>
{
}

internal static class CashSessionMapper
{
    public static CashSessionReadModel Map(CashSession session)
    {
        return new CashSessionReadModel(
            session.Id,
            session.CashierName,
            session.IsOpen,
            Money.FromNaira(session.OpeningCash),
            Money.FromNaira(session.ExpectedCash),
            session.CountedCash.HasValue ? Money.FromNaira(session.CountedCash.Value) : null,
            session.Variance.HasValue ? Money.FromNaira(session.Variance.Value) : null);
    }
}

public sealed class OpenCashSessionUseCase : IOpenCashSessionUseCase
{
    private readonly IDbContextFactory<PosDbContext> _dbContextFactory;
    private readonly IClock _clock;

    public OpenCashSessionUseCase(
        IDbContextFactory<PosDbContext> dbContextFactory,
        IClock clock)
    {
        _dbContextFactory = dbContextFactory;
        _clock = clock;
    }

    public async Task<Result<CashSessionReadModel>> ExecuteAsync(
        OpenCashSessionCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.OpeningCash.Kobo < 0)
        {
            return Result<CashSessionReadModel>.Fail("Opening cash cannot be negative.");
        }

        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        var existing = await db.CashSessions
            .FirstOrDefaultAsync(x => x.IsOpen, cancellationToken);

        if (existing is not null)
        {
            return Result<CashSessionReadModel>.Ok(CashSessionMapper.Map(existing));
        }

        var session = new CashSession
        {
            CashierId = command.CashierId,
            CashierName = command.CashierName,
            OpeningCash = command.OpeningCash.ToNaira(),
            ExpectedCash = command.OpeningCash.ToNaira(),
            IsOpen = true,
            OpenedUtc = _clock.UtcNow
        };

        db.CashSessions.Add(session);

        db.AuditLog.Add(new AuditLogEntry
        {
            CreatedUtc = _clock.UtcNow,
            UserId = command.CashierId,
            Action = "CashSessionOpened",
            Details = $"Opening cash {command.OpeningCash}."
        });

        await db.SaveChangesAsync(cancellationToken);

        return Result<CashSessionReadModel>.Ok(CashSessionMapper.Map(session));
    }
}

public sealed class CloseCashSessionUseCase : ICloseCashSessionUseCase
{
    private readonly IDbContextFactory<PosDbContext> _dbContextFactory;
    private readonly IClock _clock;

    public CloseCashSessionUseCase(
        IDbContextFactory<PosDbContext> dbContextFactory,
        IClock clock)
    {
        _dbContextFactory = dbContextFactory;
        _clock = clock;
    }

    public async Task<Result<CashSessionReadModel>> ExecuteAsync(
        CloseCashSessionCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.CountedCash.Kobo < 0)
        {
            return Result<CashSessionReadModel>.Fail("Counted cash cannot be negative.");
        }

        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        var session = await db.CashSessions
            .FirstOrDefaultAsync(x => x.IsOpen, cancellationToken);

        if (session is null)
        {
            return Result<CashSessionReadModel>.Fail("No open cash session found.");
        }

        session.IsOpen = false;
        session.CountedCash = command.CountedCash.ToNaira();
        session.Variance = command.CountedCash.ToNaira() - session.ExpectedCash;
        session.ClosedUtc = _clock.UtcNow;

        db.AuditLog.Add(new AuditLogEntry
        {
            CreatedUtc = _clock.UtcNow,
            UserId = command.UserId,
            Action = "CashSessionClosed",
            Details = $"Counted cash {command.CountedCash}. Variance {session.Variance}."
        });

        await db.SaveChangesAsync(cancellationToken);

        return Result<CashSessionReadModel>.Ok(CashSessionMapper.Map(session));
    }
}

public sealed class GetOpenCashSessionQuery : IGetOpenCashSessionQuery
{
    private readonly IDbContextFactory<PosDbContext> _dbContextFactory;

    public GetOpenCashSessionQuery(IDbContextFactory<PosDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task<Result<CashSessionReadModel?>> ExecuteAsync(
        EmptyRequest request,
        CancellationToken cancellationToken = default)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        var session = await db.CashSessions
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.IsOpen, cancellationToken);

        if (session is null)
        {
            return Result<CashSessionReadModel?>.Ok(null);
        }

        return Result<CashSessionReadModel?>.Ok(CashSessionMapper.Map(session));
    }
}