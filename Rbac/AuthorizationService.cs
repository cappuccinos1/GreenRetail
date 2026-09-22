using Microsoft.EntityFrameworkCore;
using GreenRetail.Core.Abstractions;
using GreenRetail.Data;

namespace GreenRetail.Rbac;

public interface IAuthorizationService
{
    Task<bool> HasPermissionAsync(Guid userId, string permissionCode, Guid? branchId = null, CancellationToken cancellationToken = default);
    Task<bool> HasAnyPermissionAsync(Guid userId, IEnumerable<string> permissionCodes, Guid? branchId = null, CancellationToken cancellationToken = default);
}

/// <summary>
/// Central authorization policy. Default is deny. A branch-scoped role is never
/// treated as global simply because its UserRole row was accidentally created
/// without a branch. Global roles are an explicit allow-list.
/// </summary>
public sealed class AuthorizationService : IAuthorizationService
{
    private readonly IDbContextFactory<PosDbContext> _dbContextFactory;
    private readonly IClock _clock;

    public AuthorizationService(IDbContextFactory<PosDbContext> dbContextFactory, IClock clock)
    {
        _dbContextFactory = dbContextFactory;
        _clock = clock;
    }

    public async Task<bool> HasPermissionAsync(
        Guid userId,
        string permissionCode,
        Guid? branchId = null,
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty || string.IsNullOrWhiteSpace(permissionCode))
            return false;

        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var now = _clock.UtcNow;

        // Role grants are authoritative. Global roles may operate across branches;
        // every other role must be explicitly assigned to the requested branch.
        var roleGrants = await (
            from userRole in db.UserRoles
            join role in db.Roles on userRole.RoleId equals role.Id
            join rolePermission in db.RolePermissions on role.Id equals rolePermission.RoleId
            join permission in db.Permissions on rolePermission.PermissionId equals permission.Id
            where userRole.UserId == userId && permission.Code == permissionCode
            select new { role.Name, userRole.BranchId }
        ).ToListAsync(cancellationToken);

        foreach (var grant in roleGrants)
        {
            if (RoleNames.GlobalScopeRoles.Contains(grant.Name))
            {
                if (grant.BranchId is null) return true;
                continue;
            }

            if (branchId.HasValue && grant.BranchId == branchId)
                return true;
        }

        // Explicit temporary overrides are still subject to branch scope.
        return await (
            from permissionOverride in db.PermissionOverrides
            join permission in db.Permissions on permissionOverride.PermissionId equals permission.Id
            where permissionOverride.UserId == userId
                  && permission.Code == permissionCode
                  && permissionOverride.StartUtc <= now
                  && permissionOverride.EndUtc > now
                  && (permissionOverride.BranchId == null ||
                      (branchId.HasValue && permissionOverride.BranchId == branchId))
            select permissionOverride.Id
        ).AnyAsync(cancellationToken);
    }

    public async Task<bool> HasAnyPermissionAsync(
        Guid userId,
        IEnumerable<string> permissionCodes,
        Guid? branchId = null,
        CancellationToken cancellationToken = default)
    {
        foreach (var permissionCode in permissionCodes.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (await HasPermissionAsync(userId, permissionCode, branchId, cancellationToken))
                return true;
        }

        return false;
    }
}
