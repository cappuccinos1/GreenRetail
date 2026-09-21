using Microsoft.EntityFrameworkCore;
using GreenRetail.Core.Abstractions;
using GreenRetail.Core.Results;
using GreenRetail.Data;

namespace GreenRetail.Rbac;

public sealed record GetRbacExplorerDataQueryRequest(Guid UserId);

public sealed record RoleListItem(
    string Name,
    string PermissionCount);

public sealed record PermissionListItem(
    string Code,
    string Module);

public sealed record UserPermissionCheckItem(
    string Code,
    string Granted);

public sealed record RbacExplorerData(
    IReadOnlyList<RoleListItem> Roles,
    IReadOnlyList<PermissionListItem> Permissions,
    IReadOnlyList<UserPermissionCheckItem> CurrentUserPermissions);

public interface IGetRbacExplorerDataQuery
    : IUseCase<GetRbacExplorerDataQueryRequest, Result<RbacExplorerData>>
{
}

public sealed class GetRbacExplorerDataQuery : IGetRbacExplorerDataQuery
{
    private readonly IDbContextFactory<PosDbContext> _dbContextFactory;
    private readonly IClock _clock;

    public GetRbacExplorerDataQuery(
        IDbContextFactory<PosDbContext> dbContextFactory,
        IClock clock)
    {
        _dbContextFactory = dbContextFactory;
        _clock = clock;
    }

    public async Task<Result<RbacExplorerData>> ExecuteAsync(
        GetRbacExplorerDataQueryRequest request,
        CancellationToken cancellationToken = default)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        var roles = await db.Roles
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .Select(x => new
            {
                x.Name,
                PermissionCount = db.RolePermissions.Count(rp => rp.RoleId == x.Id)
            })
            .ToListAsync(cancellationToken);

        var permissions = await db.Permissions
            .AsNoTracking()
            .OrderBy(x => x.Code)
            .Select(x => new
            {
                x.Id,
                x.Code,
                x.Module
            })
            .ToListAsync(cancellationToken);

        var rolePermissionIds = await (
            from userRole in db.UserRoles
            join rolePermission in db.RolePermissions on userRole.RoleId equals rolePermission.RoleId
            where userRole.UserId == request.UserId
            select rolePermission.PermissionId
        ).ToListAsync(cancellationToken);

        var now = _clock.UtcNow;

        var overridePermissionIds = await db.PermissionOverrides
            .AsNoTracking()
            .Where(x =>
                x.UserId == request.UserId &&
                x.StartUtc <= now &&
                x.EndUtc > now)
            .Select(x => x.PermissionId)
            .ToListAsync(cancellationToken);

        var grantedPermissionIds = new HashSet<Guid>(rolePermissionIds);
        grantedPermissionIds.UnionWith(overridePermissionIds);

        var roleList = roles
            .Select(x => new RoleListItem(x.Name, x.PermissionCount.ToString()))
            .ToList();

        var permissionList = permissions
            .Select(x => new PermissionListItem(x.Code, x.Module))
            .ToList();

        var userPermissions = permissions
            .Select(x => new UserPermissionCheckItem(
                x.Code,
                grantedPermissionIds.Contains(x.Id) ? "Yes" : "No"))
            .ToList();

        return Result<RbacExplorerData>.Ok(new RbacExplorerData(
            roleList,
            permissionList,
            userPermissions));
    }
}