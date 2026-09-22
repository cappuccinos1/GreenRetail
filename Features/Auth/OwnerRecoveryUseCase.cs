using Microsoft.EntityFrameworkCore;
using GreenRetail.Core.Results;
using GreenRetail.Data;
using GreenRetail.Rbac;

namespace GreenRetail.Features.Auth;

public sealed record OwnerRecoveryResult(string UserName, string TemporaryPassword);

public interface IOwnerRecoveryUseCase
{
    Task<Result<OwnerRecoveryResult>> ResetOwnerAccessAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Development-only local recovery for the test installation. It is deliberately
/// disabled unless GREENRETAIL_DEV_SEED=true, so it cannot become a production
/// password-reset backdoor.
/// </summary>
public sealed class OwnerRecoveryUseCase : IOwnerRecoveryUseCase
{
    private readonly IDbContextFactory<PosDbContext> _dbFactory;
    private readonly IPasswordHasher _passwordHasher;

    public OwnerRecoveryUseCase(IDbContextFactory<PosDbContext> dbFactory, IPasswordHasher passwordHasher)
    {
        _dbFactory = dbFactory;
        _passwordHasher = passwordHasher;
    }

    public async Task<Result<OwnerRecoveryResult>> ResetOwnerAccessAsync(CancellationToken cancellationToken = default)
    {
        if (!string.Equals(Environment.GetEnvironmentVariable("GREENRETAIL_DEV_SEED"), "true", StringComparison.OrdinalIgnoreCase))
            return Result<OwnerRecoveryResult>.Fail("Owner recovery is disabled. Enable GREENRETAIL_DEV_SEED=true for the local test installation.", ResultErrorCode.Authorization);

        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var owner = await db.Users.FirstOrDefaultAsync(x => x.UserName == "owner", cancellationToken);
        if (owner is null)
            return Result<OwnerRecoveryResult>.Fail("The owner account does not exist. Initialize the database first.", ResultErrorCode.NotFound);

        const string temporaryPassword = "Owner!123";
        var (salt, hash) = _passwordHasher.CreateHash(temporaryPassword);
        owner.PasswordSalt = salt;
        owner.PasswordHash = hash;
        owner.IsActive = true;
        owner.RequiresPasswordChange = true;
        owner.FailedLoginCount = 0;
        owner.LockoutEndUtc = null;
        owner.Role = RoleNames.Owner;

        var ownerRole = await db.Roles.FirstOrDefaultAsync(x => x.Name == RoleNames.Owner, cancellationToken);
        if (ownerRole is not null && !await db.UserRoles.AnyAsync(x => x.UserId == owner.Id && x.RoleId == ownerRole.Id, cancellationToken))
        {
            db.UserRoles.Add(new UserRole { UserId = owner.Id, RoleId = ownerRole.Id, AssignedUtc = DateTime.UtcNow });
        }

        db.AuditLog.Add(new Data.Entities.AuditLogEntry
        {
            UserId = owner.Id,
            CreatedUtc = DateTime.UtcNow,
            Action = "security.owner_recovery",
            Details = "Development owner credentials were reset; password change is required on next login."
        });

        await db.SaveChangesAsync(cancellationToken);
        return Result<OwnerRecoveryResult>.Ok(new OwnerRecoveryResult(owner.UserName, temporaryPassword));
    }
}
