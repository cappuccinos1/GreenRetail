using Microsoft.EntityFrameworkCore;
using GreenRetail.Core.Abstractions;
using GreenRetail.Core.Results;
using GreenRetail.Data;
using GreenRetail.Data.Entities;
using GreenRetail.Rbac;
using GreenRetail.Shared.State;

namespace GreenRetail.Features.Setup;

public sealed record CreateBranchCommand(string Code, string Name);
public sealed record CreateTerminalCommand(Guid BranchId, string Code, string Name);
public sealed record SetupResult(Guid Id, string Code, string Name);

public interface ISystemSetupUseCase
{
    Task<Result<SetupResult>> CreateBranchAsync(CreateBranchCommand command, CancellationToken cancellationToken = default);
    Task<Result<SetupResult>> CreateTerminalAsync(CreateTerminalCommand command, CancellationToken cancellationToken = default);
}

public sealed class SystemSetupUseCase : ISystemSetupUseCase
{
    private readonly IDbContextFactory<PosDbContext> _dbFactory;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuthorizationService _authorization;
    private readonly IClock _clock;

    public SystemSetupUseCase(
        IDbContextFactory<PosDbContext> dbFactory,
        ICurrentUserService currentUser,
        IAuthorizationService authorization,
        IClock clock)
    {
        _dbFactory = dbFactory;
        _currentUser = currentUser;
        _authorization = authorization;
        _clock = clock;
    }

    private async Task<Result> AuthorizeAsync(CancellationToken cancellationToken)
    {
        if (!_currentUser.UserId.HasValue)
            return Result.Fail("You must be signed in.", ResultErrorCode.Authorization);

        return await _authorization.HasPermissionAsync(
            _currentUser.UserId.Value,
            PermissionCodes.ItSystemManage,
            null,
            cancellationToken)
            ? Result.Ok()
            : Result.Fail("You do not have permission to manage system setup.", ResultErrorCode.Authorization);
    }

    public async Task<Result<SetupResult>> CreateBranchAsync(CreateBranchCommand command, CancellationToken cancellationToken = default)
    {
        var auth = await AuthorizeAsync(cancellationToken);
        if (!auth.IsSuccess) return Result<SetupResult>.Fail(auth.Error!, auth.ErrorCode);

        var code = command.Code.Trim().ToUpperInvariant();
        var name = command.Name.Trim();
        if (string.IsNullOrWhiteSpace(code) || code.Length > 20 || string.IsNullOrWhiteSpace(name))
            return Result<SetupResult>.Fail("Branch code and name are required.", ResultErrorCode.Validation);

        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        if (await db.Branches.AnyAsync(x => x.Code == code, cancellationToken))
            return Result<SetupResult>.Fail("A branch with this code already exists.", ResultErrorCode.Conflict);

        var branch = new Branch { Code = code, Name = name, IsActive = true, CreatedUtc = _clock.UtcNow };
        db.Branches.Add(branch);
        await db.SaveChangesAsync(cancellationToken);
        return Result<SetupResult>.Ok(new SetupResult(branch.Id, branch.Code, branch.Name));
    }

    public async Task<Result<SetupResult>> CreateTerminalAsync(CreateTerminalCommand command, CancellationToken cancellationToken = default)
    {
        var auth = await AuthorizeAsync(cancellationToken);
        if (!auth.IsSuccess) return Result<SetupResult>.Fail(auth.Error!, auth.ErrorCode);

        var code = command.Code.Trim().ToUpperInvariant();
        var name = command.Name.Trim();
        if (command.BranchId == Guid.Empty || string.IsNullOrWhiteSpace(code) || code.Length > 20 || string.IsNullOrWhiteSpace(name))
            return Result<SetupResult>.Fail("Branch, terminal code and terminal name are required.", ResultErrorCode.Validation);

        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        if (!await db.Branches.AnyAsync(x => x.Id == command.BranchId && x.IsActive, cancellationToken))
            return Result<SetupResult>.Fail("The selected branch does not exist or is inactive.", ResultErrorCode.NotFound);

        if (await db.Terminals.AnyAsync(x => x.Code == code, cancellationToken))
            return Result<SetupResult>.Fail("A terminal with this code already exists.", ResultErrorCode.Conflict);

        var terminal = new Terminal
        {
            BranchId = command.BranchId,
            Code = code,
            Name = name,
            IsActive = true,
            CreatedUtc = _clock.UtcNow
        };
        db.Terminals.Add(terminal);
        await db.SaveChangesAsync(cancellationToken);
        return Result<SetupResult>.Ok(new SetupResult(terminal.Id, terminal.Code, terminal.Name));
    }
}
