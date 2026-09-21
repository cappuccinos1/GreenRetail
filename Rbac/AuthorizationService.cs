using Microsoft.EntityFrameworkCore;
using GreenRetail.Core.Abstractions;
using GreenRetail.Data;

namespace GreenRetail.Rbac;

public interface IAuthorizationService
{
    Task<bool> HasPermissionAsync(
        Guid userId,
        string permissionCode,
        Guid? branchId = null,
        CancellationToken cancellationToken = default);
}

public sealed class AuthorizationService : IAuthorizationService
{
    private readonly IDbContextFactory<PosDbContext> _dbContextFactory;
    private readonly IClock _clock;

    public AuthorizationService(
        IDbContextFactory<PosDbContext> dbContextFactory,
        IClock clock)
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
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        var now = _clock.UtcNow;

        var hasRolePermission = await (
            from userRole in db.UserRoles
            join rolePermission in db.RolePermissions on userRole.RoleId equals rolePermission.RoleId
            join permission in db.Permissions on rolePermission.PermissionId equals permission.Id
            where userRole.UserId == userId
                  && permission.Code == permissionCode
                  && (userRole.BranchId == null || branchId == null || userRole.BranchId == branchId)
            select userRole
        ).AnyAsync(cancellationToken);

        if (hasRolePermission)
            return true;

        var hasOverride = await db.PermissionOverrides
            .AnyAsync(x =>
                x.UserId == userId &&
                x.Permission!.Code == permissionCode &&
                x.StartUtc <= now &&
                x.EndUtc > now,
                cancellationToken);

        return hasOverride;
    }
}