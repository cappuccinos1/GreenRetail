using Microsoft.EntityFrameworkCore;
using GreenRetail.Core.Abstractions;
using GreenRetail.Core.Results;
using GreenRetail.Core.Terminal;
using GreenRetail.Data;
using GreenRetail.Data.Entities;
using GreenRetail.Rbac;
using GreenRetail.Shared.State;

namespace GreenRetail.Features.CashSessions;

public interface IGetActiveSessionQuery : IUseCase<Guid, Result<CashSession?>> { }

public sealed class GetActiveSessionQuery : IGetActiveSessionQuery
{
    private readonly IDbContextFactory<PosDbContext> _dbFactory;
    public GetActiveSessionQuery(IDbContextFactory<PosDbContext> dbFactory) => _dbFactory = dbFactory;

    public async Task<Result<CashSession?>> ExecuteAsync(Guid terminalId, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var session = await db.CashSessions.FirstOrDefaultAsync(
            x => x.TerminalId == terminalId && (x.Status == CashSessionStatus.Open || x.Status == CashSessionStatus.Counting), ct);
        return Result<CashSession?>.Ok(session);
    }
}

public sealed record OpenRegisterCommand(Guid CashierId, string CashierName, long OpeningCashKobo);
public interface IOpenRegisterUseCase : IUseCase<OpenRegisterCommand, Result<CashSession>> { }

public sealed class OpenRegisterUseCase : IOpenRegisterUseCase
{
    private readonly IDbContextFactory<PosDbContext> _dbFactory;
    private readonly ITerminalContext _terminal;
    private readonly ICurrentUserService _currentUser;
    private readonly IClock _clock;
    private readonly IAuthorizationService _authorization;

    public OpenRegisterUseCase(IDbContextFactory<PosDbContext> dbFactory, ITerminalContext terminal,
        ICurrentUserService currentUser, IClock clock, IAuthorizationService authorization)
    {
        _dbFactory = dbFactory; _terminal = terminal; _currentUser = currentUser; _clock = clock; _authorization = authorization;
    }

    public async Task<Result<CashSession>> ExecuteAsync(OpenRegisterCommand cmd, CancellationToken ct = default)
    {
        if (cmd.OpeningCashKobo < 0) return Result<CashSession>.Fail("Opening cash cannot be negative.");
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            return Result<CashSession>.Fail("You must be signed in to open the register.");
        if (_terminal.BranchId is null)
            return Result<CashSession>.Fail("This terminal is not assigned to a branch.");
        if (!await _authorization.HasPermissionAsync(_currentUser.UserId.Value, PermissionCodes.PosSessionOpen, _terminal.BranchId, ct))
            return Result<CashSession>.Fail("You do not have permission to open the register.", ResultErrorCode.Authorization);

        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var cashier = await db.Users.FirstOrDefaultAsync(x => x.Id == _currentUser.UserId.Value && x.IsActive, ct);
        if (cashier is null) return Result<CashSession>.Fail("The signed-in user is not an active user.");
        if (await db.CashSessions.AnyAsync(x => x.TerminalId == _terminal.TerminalId &&
            (x.Status == CashSessionStatus.Open || x.Status == CashSessionStatus.Counting), ct))
            return Result<CashSession>.Fail("A cash session is already active on this register.");

        var session = new CashSession
        {
            TerminalId = _terminal.TerminalId,
            BranchId = _terminal.BranchId.Value,
            CashierId = cashier.Id,
            CashierName = cashier.Name,
            OpeningCashKobo = cmd.OpeningCashKobo,
            ExpectedCashKobo = cmd.OpeningCashKobo,
            Status = CashSessionStatus.Open,
            OpenedUtc = _clock.UtcNow
        };
        db.CashSessions.Add(session);
        db.AuditLog.Add(new AuditLogEntry { UserId = _currentUser.UserId, CreatedUtc = _clock.UtcNow,
            Action = "cash.session.open", Details = $"Session={session.Id}; Terminal={session.TerminalId}; OpeningCashKobo={session.OpeningCashKobo}" });
        await db.SaveChangesAsync(ct);
        return Result<CashSession>.Ok(session);
    }
}

public sealed record CloseRegisterCommand(Guid CountedByUserId, long CountedCashKobo);
public interface ICloseRegisterUseCase : IUseCase<CloseRegisterCommand, Result<CashSession>> { }

/// <summary>
/// Cashier Manager requests the session enter blind-counting state.
/// Accounts performs the actual count. The two operations share this use case
/// for compatibility with the existing UI/API surface, but are permission-gated
/// and cannot be substituted for one another.
/// </summary>
public sealed class CloseRegisterUseCase : ICloseRegisterUseCase
{
    private const long VarianceToleranceKobo = 500_000; // ₦5,000
    private readonly IDbContextFactory<PosDbContext> _dbFactory;
    private readonly ITerminalContext _terminal;
    private readonly ICurrentUserService _currentUser;
    private readonly IClock _clock;
    private readonly IAuthorizationService _authorization;

    public CloseRegisterUseCase(IDbContextFactory<PosDbContext> dbFactory, ITerminalContext terminal,
        ICurrentUserService currentUser, IClock clock, IAuthorizationService authorization)
    {
        _dbFactory = dbFactory; _terminal = terminal; _currentUser = currentUser; _clock = clock; _authorization = authorization;
    }

    public async Task<Result<CashSession>> ExecuteAsync(CloseRegisterCommand cmd, CancellationToken ct = default)
    {
        if (!_currentUser.IsAuthenticated || !_currentUser.UserId.HasValue)
            return Result<CashSession>.Fail("You must be signed in to manage cash sessions.");
        if (_terminal.BranchId is null)
            return Result<CashSession>.Fail("This terminal is not assigned to a branch.");

        var userId = _currentUser.UserId.Value;
        var canRequestClose = await _authorization.HasPermissionAsync(userId, PermissionCodes.PosSessionClose, _terminal.BranchId, ct);
        var canCount = await _authorization.HasPermissionAsync(userId, PermissionCodes.CashCount, _terminal.BranchId, ct);
        if (!canRequestClose && !canCount)
            return Result<CashSession>.Fail("You do not have permission to close or count this cash session.", ResultErrorCode.Authorization);

        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var session = await db.CashSessions.FirstOrDefaultAsync(x => x.TerminalId == _terminal.TerminalId &&
            (x.Status == CashSessionStatus.Open || x.Status == CashSessionStatus.Counting), ct);
        if (session is null) return Result<CashSession>.Fail("No active cash session found on this register.");

        // Manager operation: request blind count. No cash amount is accepted here.
        if (canRequestClose && !canCount)
        {
            if (session.Status != CashSessionStatus.Open)
                return Result<CashSession>.Fail("This session is already awaiting an Accounts blind count.");
            session.Status = CashSessionStatus.Counting;
            db.AuditLog.Add(new AuditLogEntry { UserId = userId, CreatedUtc = _clock.UtcNow,
                Action = "cash.session.count_requested", Details = $"Session={session.Id}; Branch={session.BranchId}; Terminal={session.TerminalId}" });
            await db.SaveChangesAsync(ct);
            return Result<CashSession>.Ok(session);
        }

        // Accounts operation: record the blind count.
        if (cmd.CountedCashKobo < 0) return Result<CashSession>.Fail("Counted cash cannot be negative.");
        if (session.Status != CashSessionStatus.Counting)
            return Result<CashSession>.Fail("The manager must send this session for counting before Accounts can record a count.");

        session.CountedCashKobo = cmd.CountedCashKobo;
        session.VarianceKobo = cmd.CountedCashKobo - session.ExpectedCashKobo;
        session.CountedByUserId = userId;

        var withinTolerance = Math.Abs(session.VarianceKobo.Value) <= VarianceToleranceKobo;
        if (withinTolerance)
        {
            session.Status = CashSessionStatus.Closed;
            session.ClosedUtc = _clock.UtcNow;
        }

        db.AuditLog.Add(new AuditLogEntry { UserId = userId, CreatedUtc = _clock.UtcNow,
            Action = "cash.session.blind_count", Details = $"Session={session.Id}; CountedCashKobo={session.CountedCashKobo}; VarianceKobo={session.VarianceKobo}; AutoClosed={withinTolerance}" });
        await db.SaveChangesAsync(ct);
        return Result<CashSession>.Ok(session);
    }
}

public sealed record ApproveCashVarianceCommand(Guid SessionId);
public interface IApproveCashVarianceUseCase : IUseCase<ApproveCashVarianceCommand, Result<CashSession>> { }

public sealed class ApproveCashVarianceUseCase : IApproveCashVarianceUseCase
{
    private const long VarianceToleranceKobo = 500_000;
    private readonly IDbContextFactory<PosDbContext> _dbFactory;
    private readonly ITerminalContext _terminal;
    private readonly ICurrentUserService _currentUser;
    private readonly IClock _clock;
    private readonly IAuthorizationService _authorization;

    public ApproveCashVarianceUseCase(IDbContextFactory<PosDbContext> dbFactory, ITerminalContext terminal,
        ICurrentUserService currentUser, IClock clock, IAuthorizationService authorization)
    { _dbFactory = dbFactory; _terminal = terminal; _currentUser = currentUser; _clock = clock; _authorization = authorization; }

    public async Task<Result<CashSession>> ExecuteAsync(ApproveCashVarianceCommand cmd, CancellationToken ct = default)
    {
        if (!_currentUser.UserId.HasValue) return Result<CashSession>.Fail("You must be signed in.");
        if (_terminal.BranchId is null) return Result<CashSession>.Fail("This terminal is not assigned to a branch.");
        if (!await _authorization.HasPermissionAsync(_currentUser.UserId.Value, PermissionCodes.CashVarianceApprove, _terminal.BranchId, ct))
            return Result<CashSession>.Fail("You do not have permission to approve cash variance.", ResultErrorCode.Authorization);

        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var session = await db.CashSessions.FirstOrDefaultAsync(x => x.Id == cmd.SessionId && x.TerminalId == _terminal.TerminalId, ct);
        if (session is null) return Result<CashSession>.Fail("Cash session not found on this register.");
        if (session.Status != CashSessionStatus.Counting || session.VarianceKobo is null || session.CountedCashKobo is null)
            return Result<CashSession>.Fail("This session is not awaiting variance approval.");
        if (Math.Abs(session.VarianceKobo.Value) <= VarianceToleranceKobo)
            return Result<CashSession>.Fail("This variance is already within the automatic tolerance and does not require approval.");

        session.Status = CashSessionStatus.Closed;
        session.ClosedUtc = _clock.UtcNow;
        db.AuditLog.Add(new AuditLogEntry { UserId = _currentUser.UserId, CreatedUtc = _clock.UtcNow,
            Action = "cash.session.variance_approved", Details = $"Session={session.Id}; VarianceKobo={session.VarianceKobo}" });
        await db.SaveChangesAsync(ct);
        return Result<CashSession>.Ok(session);
    }
}
