using Microsoft.EntityFrameworkCore;
using GreenRetail.Core.Abstractions;
using GreenRetail.Core.Results;
using GreenRetail.Core.Terminal;
using GreenRetail.Data;
using GreenRetail.Data.Entities;
using GreenRetail.Shared.State;

namespace GreenRetail.Features.CashSessions;

// --- Queries ---
public interface IGetActiveSessionQuery : IUseCase<Guid, Result<CashSession?>> { }

public sealed class GetActiveSessionQuery : IGetActiveSessionQuery
{
    private readonly IDbContextFactory<PosDbContext> _dbFactory;

    public GetActiveSessionQuery(IDbContextFactory<PosDbContext> dbFactory)
        => _dbFactory = dbFactory;

    public async Task<Result<CashSession?>> ExecuteAsync(Guid terminalId, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var session = await db.CashSessions
            .FirstOrDefaultAsync(
                x => x.TerminalId == terminalId && x.Status == CashSessionStatus.Open,
                ct);

        return Result<CashSession?>.Ok(session);
    }
}

// --- Open Register (Manager Only) ---
public sealed record OpenRegisterCommand(Guid CashierId, string CashierName, long OpeningCashKobo); // legacy fields retained for caller compatibility; server uses signed-in user
public interface IOpenRegisterUseCase : IUseCase<OpenRegisterCommand, Result<CashSession>> { }

public sealed class OpenRegisterUseCase : IOpenRegisterUseCase
{
    private readonly IDbContextFactory<PosDbContext> _dbFactory;
    private readonly ITerminalContext _terminal;
    private readonly ICurrentUserService _currentUser;
    private readonly IClock _clock;
    private readonly GreenRetail.Rbac.IAuthorizationService _authorization;

    public OpenRegisterUseCase(IDbContextFactory<PosDbContext> dbFactory, ITerminalContext terminal, ICurrentUserService currentUser, IClock clock, GreenRetail.Rbac.IAuthorizationService authorization)
    {
        _dbFactory = dbFactory;
        _terminal = terminal;
        _currentUser = currentUser;
        _clock = clock;
        _authorization = authorization;
    }

    public async Task<Result<CashSession>> ExecuteAsync(OpenRegisterCommand cmd, CancellationToken ct = default)
    {
        if (cmd.OpeningCashKobo < 0)
            return Result<CashSession>.Fail("Opening cash cannot be negative.");

        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            return Result<CashSession>.Fail("You must be signed in to open the register.");

        if (!await _authorization.HasPermissionAsync(_currentUser.UserId.Value, GreenRetail.Rbac.PermissionCodes.PosSessionOpen, _terminal.BranchId, ct))
            return Result<CashSession>.Fail("You do not have permission to open the register.", ResultErrorCode.Authorization);

        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        
        if (_terminal.BranchId is null)
            return Result<CashSession>.Fail("This terminal is not assigned to a branch.");

        var cashier = await db.Users.FirstOrDefaultAsync(x => x.Id == _currentUser.UserId.Value && x.IsActive, ct);
        if (cashier is null)
            return Result<CashSession>.Fail("The signed-in user is not an active cashier/manager account.");

        // Rule: Only one open session per terminal
        var existing = await db.CashSessions.AnyAsync(x => x.TerminalId == _terminal.TerminalId && x.Status == CashSessionStatus.Open, ct);
        if (existing) return Result<CashSession>.Fail("A session is already open on this register.");

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
        await db.SaveChangesAsync(ct);
        return Result<CashSession>.Ok(session);
    }
}

// --- Close Register (Blind Count) ---
public sealed record CloseRegisterCommand(Guid CountedByUserId, long CountedCashKobo); // legacy identity field retained; server uses signed-in user
public interface ICloseRegisterUseCase : IUseCase<CloseRegisterCommand, Result<CashSession>> { }

public sealed class CloseRegisterUseCase : ICloseRegisterUseCase
{
    private readonly IDbContextFactory<PosDbContext> _dbFactory;
    private readonly ITerminalContext _terminal;
    private readonly ICurrentUserService _currentUser;
    private readonly IClock _clock;
    private readonly GreenRetail.Rbac.IAuthorizationService _authorization;

    public CloseRegisterUseCase(IDbContextFactory<PosDbContext> dbFactory, ITerminalContext terminal, ICurrentUserService currentUser, IClock clock, GreenRetail.Rbac.IAuthorizationService authorization)
    {
        _dbFactory = dbFactory;
        _terminal = terminal;
        _currentUser = currentUser;
        _clock = clock;
        _authorization = authorization;
    }

    public async Task<Result<CashSession>> ExecuteAsync(CloseRegisterCommand cmd, CancellationToken ct = default)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            return Result<CashSession>.Fail("You must be signed in to close the register.");

        if (!await _authorization.HasPermissionAsync(_currentUser.UserId.Value, GreenRetail.Rbac.PermissionCodes.PosSessionClose, _terminal.BranchId, ct))
            return Result<CashSession>.Fail("You do not have permission to close the register.", ResultErrorCode.Authorization);

        if (cmd.CountedCashKobo < 0)
            return Result<CashSession>.Fail("Counted cash cannot be negative.");

        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var session = await db.CashSessions.FirstOrDefaultAsync(x => x.TerminalId == _terminal.TerminalId && x.Status == CashSessionStatus.Open, ct);
        
        if (session is null) return Result<CashSession>.Fail("No open session found on this register.");

        session.CountedCashKobo = cmd.CountedCashKobo;
        session.VarianceKobo = cmd.CountedCashKobo - session.ExpectedCashKobo;
        session.CountedByUserId = _currentUser.UserId.Value;
        session.Status = CashSessionStatus.Closed;
        session.ClosedUtc = _clock.UtcNow;

        await db.SaveChangesAsync(ct);
        return Result<CashSession>.Ok(session);
    }
}