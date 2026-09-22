using Microsoft.EntityFrameworkCore;
using GreenRetail.Core.Abstractions;
using GreenRetail.Core.Results;
using GreenRetail.Data;
using GreenRetail.Shared.State;

namespace GreenRetail.Rbac;

public sealed record GrantPermissionOverrideCommand(
    Guid TargetUserId,
    string PermissionCode,
    Guid? BranchId,
    DateTime StartUtc,
    DateTime EndUtc,
    string Reason);

public interface IGrantPermissionOverrideUseCase
    : IUseCase<GrantPermissionOverrideCommand, Result<Guid>>
{
}

/// <summary>
/// The normal permission matrix is default-deny. This is the controlled escape
/// hatch for temporary operational exceptions. Only Owner, IT Head or HR Head
/// may grant an override, represented by their dedicated administrative
/// permissions rather than by hard-coded role checks.
/// </summary>
public sealed class GrantPermissionOverrideUseCase : IGrantPermissionOverrideUseCase
{
    private readonly IDbContextFactory<PosDbContext> _dbFactory;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuthorizationService _authorization;
    private readonly IClock _clock;

    public GrantPermissionOverrideUseCase(
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

    public async Task<Result<Guid>> ExecuteAsync(
        GrantPermissionOverrideCommand command,
        CancellationToken cancellationToken = default)
    {
        if (!_currentUser.UserId.HasValue)
            return Result<Guid>.Fail("You must be signed in.", ResultErrorCode.Authorization);

        if (command.TargetUserId == Guid.Empty || string.IsNullOrWhiteSpace(command.PermissionCode))
            return Result<Guid>.Fail("Target user and permission are required.", ResultErrorCode.Validation);

        if (string.IsNullOrWhiteSpace(command.Reason))
            return Result<Guid>.Fail("An override reason is required.", ResultErrorCode.Validation);

        if (command.EndUtc <= command.StartUtc || command.EndUtc <= _clock.UtcNow)
            return Result<Guid>.Fail("Override expiry must be after its start and in the future.", ResultErrorCode.Validation);

        var grantorId = _currentUser.UserId.Value;
        var canOverride = await _authorization.HasAnyPermissionAsync(
            grantorId,
            new[] { PermissionCodes.ItSystemManage, PermissionCodes.HrRoleAssign },
            command.BranchId,
            cancellationToken);

        if (!canOverride)
            return Result<Guid>.Fail("Only the Owner, IT Head or HR Head may grant permission overrides.", ResultErrorCode.Authorization);

        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);

        var target = await db.Users.FirstOrDefaultAsync(x => x.Id == command.TargetUserId && x.IsActive, cancellationToken);
        if (target is null)
            return Result<Guid>.Fail("Target user was not found or is inactive.", ResultErrorCode.NotFound);

        var permission = await db.Permissions.FirstOrDefaultAsync(x => x.Code == command.PermissionCode, cancellationToken);
        if (permission is null)
            return Result<Guid>.Fail("Permission was not found.", ResultErrorCode.NotFound);

        if (command.BranchId.HasValue && !await db.Branches.AnyAsync(x => x.Id == command.BranchId && x.IsActive, cancellationToken))
            return Result<Guid>.Fail("Override branch was not found or is inactive.", ResultErrorCode.NotFound);

        var overrideEntity = new PermissionOverride
        {
            UserId = target.Id,
            PermissionId = permission.Id,
            BranchId = command.BranchId,
            GrantedById = grantorId,
            Reason = command.Reason.Trim(),
            StartUtc = command.StartUtc,
            EndUtc = command.EndUtc,
            CreatedUtc = _clock.UtcNow
        };

        db.PermissionOverrides.Add(overrideEntity);
        db.AuditLog.Add(new Data.Entities.AuditLogEntry
        {
            CreatedUtc = _clock.UtcNow,
            UserId = grantorId,
            Action = "PermissionOverrideGranted",
            Details = $"Permission '{permission.Code}' granted to user '{target.UserName}' until {command.EndUtc:O}. Reason: {command.Reason.Trim()}"
        });

        await db.SaveChangesAsync(cancellationToken);
        return Result<Guid>.Ok(overrideEntity.Id);
    }
}
