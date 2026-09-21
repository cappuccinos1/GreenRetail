using Microsoft.EntityFrameworkCore;
using GreenRetail.Data;

namespace GreenRetail.Rbac;

public static class RbacSeeder
{
    public static async Task SeedAsync(PosDbContext db, CancellationToken cancellationToken = default)
    {
        await SeedPermissionsAsync(db, cancellationToken);
        await SeedRolesAsync(db, cancellationToken);
        await SeedRolePermissionsAsync(db, cancellationToken);
        await SeedOwnerRoleAsync(db, cancellationToken);
    }

    private static async Task SeedPermissionsAsync(PosDbContext db, CancellationToken cancellationToken)
    {
        if (await db.Permissions.AnyAsync(cancellationToken))
            return;

        foreach (var code in PermissionCodes.All)
        {
            db.Permissions.Add(new Permission
            {
                Code = code,
                Module = code.Split('.')[0],
                Description = code,
                CreatedUtc = DateTime.UtcNow
            });
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task SeedRolesAsync(PosDbContext db, CancellationToken cancellationToken)
    {
        if (await db.Roles.AnyAsync(cancellationToken))
            return;

        var roles = new[]
        {
            "Owner",
            "ITAdmin",
            "HROfficer",
            "HRHead",
            "BranchManager",
            "Cashier",
            "CashierManager",
            "InventoryOfficer",
            "InventoryHead",
            "Buyer",
            "QCOfficer",
            "QCHead",
            "AccountsOfficer",
            "AccountsHead",
            "AuditOfficer",
            "AuditHead",
            "SecurityOfficer"
        };

        foreach (var role in roles)
        {
            db.Roles.Add(new Role
            {
                Name = role,
                Description = role,
                IsSystem = true,
                CreatedUtc = DateTime.UtcNow
            });
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task SeedRolePermissionsAsync(PosDbContext db, CancellationToken cancellationToken)
    {
        if (await db.RolePermissions.AnyAsync(cancellationToken))
            return;

        var roles = await db.Roles.ToDictionaryAsync(x => x.Name, cancellationToken);
        var permissions = await db.Permissions.ToDictionaryAsync(x => x.Code, cancellationToken);

        var grants = new Dictionary<string, string[]>
        {
            ["Owner"] = PermissionCodes.All,

            ["ITAdmin"] = new[]
            {
                PermissionCodes.ItUserCreate,
                PermissionCodes.ItSystemManage
            },

            ["HROfficer"] = new[]
            {
                PermissionCodes.HrUserRequest
            },

            ["HRHead"] = new[]
            {
                PermissionCodes.HrUserRequest,
                PermissionCodes.HrRoleAssign
            },

            ["BranchManager"] = new[]
            {
                PermissionCodes.InventoryStockView,
                PermissionCodes.SecurityIncidentCreate
            },

            ["Cashier"] = new[]
            {
                PermissionCodes.PosSaleCreate,
                PermissionCodes.PosSaleSuspend
            },

            ["CashierManager"] = new[]
            {
                PermissionCodes.PosSessionOpen,
                PermissionCodes.PosSessionClose,
                PermissionCodes.PosRefundInitiate,
                PermissionCodes.PosRefundApprove,
                PermissionCodes.PosReceiptReprint,
                PermissionCodes.StoreCreditIssue
            },

            ["InventoryOfficer"] = new[]
            {
                PermissionCodes.InventoryProductManage,
                PermissionCodes.InventoryStockView,
                PermissionCodes.InventoryStockAdjustRequest,
                PermissionCodes.InventorySupplierManage,
                PermissionCodes.InventoryStockOverrideResolve
            },

            ["InventoryHead"] = new[]
            {
                PermissionCodes.InventoryProductManage,
                PermissionCodes.InventoryStockView,
                PermissionCodes.InventoryStockAdjustRequest,
                PermissionCodes.InventoryStockAdjustApprove,
                PermissionCodes.InventorySupplierManage,
                PermissionCodes.InventoryStockOverrideResolve
            },

            ["Buyer"] = new[]
            {
                PermissionCodes.PurchasingPoCreate,
                PermissionCodes.PurchasingPoView
            },

            ["QCOfficer"] = new[]
            {
                PermissionCodes.QcInspect
            },

            ["QCHead"] = new[]
            {
                PermissionCodes.QcInspect,
                PermissionCodes.QcApprove
            },

            ["AccountsOfficer"] = new[]
            {
                PermissionCodes.CashCount,
                PermissionCodes.CashVarianceApprove,
                PermissionCodes.CashDepositRecord,
                PermissionCodes.FinanceView
            },

            ["AccountsHead"] = new[]
            {
                PermissionCodes.CashCount,
                PermissionCodes.CashVarianceApprove,
                PermissionCodes.CashDepositRecord,
                PermissionCodes.FinanceView,
                PermissionCodes.FinanceStatementPrepare
            },

            ["AuditOfficer"] = new[]
            {
                PermissionCodes.AuditView
            },

            ["AuditHead"] = new[]
            {
                PermissionCodes.AuditView,
                PermissionCodes.AuditCaseManage
            },

            ["SecurityOfficer"] = new[]
            {
                PermissionCodes.SecurityReceiptVerify,
                PermissionCodes.SecurityIncidentCreate
            }
        };

        foreach (var grant in grants)
        {
            var role = roles[grant.Key];

            foreach (var permissionCode in grant.Value)
            {
                db.RolePermissions.Add(new RolePermission
                {
                    RoleId = role.Id,
                    PermissionId = permissions[permissionCode].Id
                });
            }
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task SeedOwnerRoleAsync(PosDbContext db, CancellationToken cancellationToken)
    {
        var ownerUser = await db.Users
            .FirstOrDefaultAsync(x => x.UserName == "owner", cancellationToken);

        if (ownerUser is null)
            return;

        var ownerRole = await db.Roles
            .FirstOrDefaultAsync(x => x.Name == "Owner", cancellationToken);

        if (ownerRole is null)
            return;

        var alreadyAssigned = await db.UserRoles
            .AnyAsync(x => x.UserId == ownerUser.Id && x.RoleId == ownerRole.Id, cancellationToken);

        if (alreadyAssigned)
            return;

        db.UserRoles.Add(new UserRole
        {
            UserId = ownerUser.Id,
            RoleId = ownerRole.Id,
            AssignedUtc = DateTime.UtcNow
        });

        await db.SaveChangesAsync(cancellationToken);
    }
}