using Microsoft.EntityFrameworkCore;
using GreenRetail.Core.Abstractions;
using GreenRetail.Data;
using GreenRetail.Data.Entities;

namespace GreenRetail.Rbac;

/// <summary>
/// Idempotent RBAC synchronizer. Unlike the old one-shot seeder, this deliberately
/// reconciles system roles/permissions so a hardened build can correct an earlier
/// permission matrix without requiring a fresh database.
/// </summary>
public static class RbacSeeder
{
    public static async Task SeedAsync(PosDbContext db, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        await EnsurePermissionsAsync(db, now, cancellationToken);
        await EnsureRolesAsync(db, now, cancellationToken);
        await ReconcileSystemRolePermissionsAsync(db, cancellationToken);
        await EnsureOwnerRoleAsync(db, now, cancellationToken);
        await MigrateLegacyItRoleAsync(db, now, cancellationToken);
        await MigrateLegacyQualityControlRolesAsync(db, now, cancellationToken);
    }

    private static async Task EnsurePermissionsAsync(PosDbContext db, DateTime now, CancellationToken ct)
    {
        var existingRows = await db.Permissions.ToListAsync(ct);
        var existing = existingRows.ToDictionary(x => x.Code, StringComparer.OrdinalIgnoreCase);
        foreach (var code in PermissionCodes.All)
        {
            if (existing.ContainsKey(code)) continue;
            db.Permissions.Add(new Permission
            {
                Code = code,
                Module = code.Split('.')[0],
                Description = code,
                CreatedUtc = now
            });
        }
        await db.SaveChangesAsync(ct);
    }

    private static async Task EnsureRolesAsync(PosDbContext db, DateTime now, CancellationToken ct)
    {
        var roleNames = new[]
        {
            RoleNames.Owner,
            RoleNames.ITHead,
            RoleNames.HROfficer,
            RoleNames.HRHead,
            RoleNames.BranchManager,
            RoleNames.Cashier,
            RoleNames.CashierManager,
            RoleNames.InventoryOfficer,
            RoleNames.InventoryHead,
            RoleNames.Buyer,
            RoleNames.ProcurementHead,
            RoleNames.QualityControlOfficer,
            RoleNames.QualityControlHead,
            RoleNames.AccountsOfficer,
            RoleNames.AccountsHead,
            RoleNames.AccountsGroupHead,
            RoleNames.AuditOfficer,
            RoleNames.AuditHead,
            RoleNames.SecurityOfficer
        };

        var existingRows = await db.Roles.ToListAsync(ct);
        var existing = existingRows.ToDictionary(x => x.Name, StringComparer.OrdinalIgnoreCase);
        foreach (var name in roleNames)
        {
            if (existing.ContainsKey(name)) continue;
            db.Roles.Add(new Role
            {
                Name = name,
                Description = name,
                IsSystem = true,
                CreatedUtc = now
            });
        }
        await db.SaveChangesAsync(ct);
    }

    private static Dictionary<string, string[]> Matrix => new(StringComparer.OrdinalIgnoreCase)
    {
        [RoleNames.Owner] = PermissionCodes.All,

        // IT implements the access decisions; HR requests/assigns. This keeps
        // user creation and role implementation separate from operational duties.
        [RoleNames.ITHead] = new[]
        {
            PermissionCodes.ItUserCreate,
            PermissionCodes.ItSystemManage,
            PermissionCodes.ItRoleImplement
        },
        [RoleNames.HROfficer] = new[]
        {
            PermissionCodes.HrUserRequest,
            PermissionCodes.HrRoleChangeRequest
        },
        [RoleNames.HRHead] = new[]
        {
            PermissionCodes.HrUserRequest,
            PermissionCodes.HrRoleAssign,
            PermissionCodes.HrRoleChangeRequest
        },

        // Branch managers deliberately have no cash, sales figures, profit,
        // purchasing, pricing or product-creation authority.
        [RoleNames.BranchManager] = new[]
        {
            PermissionCodes.InventoryStockView,
            PermissionCodes.SecurityIncidentCreate
        },

        [RoleNames.Cashier] = new[]
        {
            PermissionCodes.PosSaleCreate,
            PermissionCodes.PosSaleSuspend,
            PermissionCodes.PosStockOverrideRequest
        },
        [RoleNames.CashierManager] = new[]
        {
            PermissionCodes.PosSessionOpen,
            PermissionCodes.PosSessionClose,
            PermissionCodes.PosRefundInitiate,
            PermissionCodes.PosRefundApprove,
            PermissionCodes.PosRefundHighValueApprove,
            PermissionCodes.PosReceiptReprint,
            PermissionCodes.PosStockOverrideRequest,
            PermissionCodes.ReportsCashierPerformanceView,
            PermissionCodes.StoreCreditIssue
        },

        // Pricing/category management remains an inventory responsibility.
        [RoleNames.InventoryOfficer] = new[]
        {
            PermissionCodes.InventoryProductManage,
            PermissionCodes.InventoryStockView,
            PermissionCodes.InventoryStockAdjustRequest,
            PermissionCodes.InventorySupplierManage,
            PermissionCodes.InventoryStockOverrideResolve,
            PermissionCodes.InventoryTransferCreate,
            PermissionCodes.InventoryQuarantineManage
        },
        [RoleNames.InventoryHead] = new[]
        {
            PermissionCodes.InventoryProductManage,
            PermissionCodes.InventoryStockView,
            PermissionCodes.InventoryStockAdjustRequest,
            PermissionCodes.InventoryStockAdjustApprove,
            PermissionCodes.InventorySupplierManage,
            PermissionCodes.InventoryStockOverrideResolve,
            PermissionCodes.InventoryTransferCreate,
            PermissionCodes.InventoryTransferApprove,
            PermissionCodes.InventoryNegativeStockAllow,
            PermissionCodes.InventoryQuarantineManage
        },

        [RoleNames.Buyer] = new[]
        {
            PermissionCodes.PurchasingPoCreate,
            PermissionCodes.PurchasingPoView,
            PermissionCodes.ReceivingNoPoConfirm
        },
        [RoleNames.ProcurementHead] = new[]
        {
            PermissionCodes.PurchasingPoCreate,
            PermissionCodes.PurchasingPoView,
            PermissionCodes.ReceivingNoPoConfirm
        },

        // Quality Control inspects; it never writes stock. Inventory posts the accepted GRN.
        [RoleNames.QualityControlOfficer] = new[] { PermissionCodes.QualityControlInspect, PermissionCodes.ReceivingInvoiceWithoutPo },
        [RoleNames.QualityControlHead] = new[] { PermissionCodes.QualityControlInspect, PermissionCodes.QualityControlApprove, PermissionCodes.ReceivingInvoiceWithoutPo },

        // Cash and financial visibility belong to Accounts.
        [RoleNames.AccountsOfficer] = new[]
        {
            PermissionCodes.CashCount,
            PermissionCodes.CashVarianceApprove,
            PermissionCodes.CashDepositRecord,
            PermissionCodes.FinanceView,
            PermissionCodes.ReportsSalesView
        },
        [RoleNames.AccountsHead] = new[]
        {
            PermissionCodes.CashCount,
            PermissionCodes.CashVarianceApprove,
            PermissionCodes.CashDepositRecord,
            PermissionCodes.FinanceView,
            PermissionCodes.FinanceStatementPrepare,
            PermissionCodes.ReportsSalesView
        },
        // Optional organisation-level Accounts role. Branch Accounts Heads remain
        // branch-scoped; this role is intentionally global.
        [RoleNames.AccountsGroupHead] = new[]
        {
            PermissionCodes.CashCount,
            PermissionCodes.CashVarianceApprove,
            PermissionCodes.CashDepositRecord,
            PermissionCodes.FinanceView,
            PermissionCodes.FinanceStatementPrepare,
            PermissionCodes.FinanceOrganisationStatementPrepare,
            PermissionCodes.ReportsSalesView
        },

        // Audit recommends/investigates; it does not execute operational changes.
        [RoleNames.AuditOfficer] = new[] { PermissionCodes.AuditView, PermissionCodes.AuditRecommend },
        [RoleNames.AuditHead] = new[] { PermissionCodes.AuditView, PermissionCodes.AuditCaseManage, PermissionCodes.AuditRecommend },

        [RoleNames.SecurityOfficer] = new[]
        {
            PermissionCodes.SecurityReceiptVerify,
            PermissionCodes.SecurityIncidentCreate
        }
    };

    private static async Task ReconcileSystemRolePermissionsAsync(PosDbContext db, CancellationToken ct)
    {
        var roleRows = await db.Roles.Where(x => x.IsSystem).ToListAsync(ct);
        var roles = roleRows.ToDictionary(x => x.Name, StringComparer.OrdinalIgnoreCase);
        var permissionRows = await db.Permissions.ToListAsync(ct);
        var permissions = permissionRows.ToDictionary(x => x.Code, StringComparer.OrdinalIgnoreCase);
        var existing = await db.RolePermissions.ToListAsync(ct);

        foreach (var matrixEntry in Matrix)
        {
            if (!roles.TryGetValue(matrixEntry.Key, out var role)) continue;

            // System-role permissions are declarative. Remove stale grants so a
            // previously broader role cannot retain privileges after an upgrade.
            var desired = matrixEntry.Value.ToHashSet(StringComparer.OrdinalIgnoreCase);
            var stale = existing.Where(x => x.RoleId == role.Id &&
                !permissions.Values.Any(p => p.Id == x.PermissionId && desired.Contains(p.Code))).ToList();
            if (stale.Count > 0) db.RolePermissions.RemoveRange(stale);

            var currentPermissionIds = existing.Where(x => x.RoleId == role.Id).Select(x => x.PermissionId).ToHashSet();
            foreach (var code in matrixEntry.Value)
            {
                if (!permissions.TryGetValue(code, out var permission) || currentPermissionIds.Contains(permission.Id)) continue;
                db.RolePermissions.Add(new RolePermission { RoleId = role.Id, PermissionId = permission.Id });
            }
        }

        await db.SaveChangesAsync(ct);
    }

    private static async Task EnsureOwnerRoleAsync(PosDbContext db, DateTime now, CancellationToken ct)
    {
        var ownerUser = await db.Users.FirstOrDefaultAsync(x => x.UserName == "owner", ct);
        var ownerRole = await db.Roles.FirstOrDefaultAsync(x => x.Name == RoleNames.Owner, ct);
        if (ownerUser is null || ownerRole is null) return;

        if (!await db.UserRoles.AnyAsync(x => x.UserId == ownerUser.Id && x.RoleId == ownerRole.Id, ct))
        {
            db.UserRoles.Add(new UserRole
            {
                UserId = ownerUser.Id,
                RoleId = ownerRole.Id,
                AssignedUtc = now
            });
            await db.SaveChangesAsync(ct);
        }
    }

    private static async Task MigrateLegacyItRoleAsync(PosDbContext db, DateTime now, CancellationToken ct)
    {
        // Older builds called the system role ITAdmin. Preserve existing users by
        // assigning the new ITHead role; do not delete the legacy role in place.
        var legacy = await db.Roles.FirstOrDefaultAsync(x => x.Name == "ITAdmin", ct);
        var current = await db.Roles.FirstOrDefaultAsync(x => x.Name == RoleNames.ITHead, ct);
        if (legacy is null || current is null) return;

        var legacyUsers = await db.UserRoles.Where(x => x.RoleId == legacy.Id).ToListAsync(ct);
        foreach (var assignment in legacyUsers)
        {
            if (!await db.UserRoles.AnyAsync(x => x.UserId == assignment.UserId && x.RoleId == current.Id, ct))
            {
                db.UserRoles.Add(new UserRole
                {
                    UserId = assignment.UserId,
                    RoleId = current.Id,
                    BranchId = null,
                    AssignedUtc = now
                });
            }
        }
        await db.SaveChangesAsync(ct);
    }
    private static async Task MigrateLegacyQualityControlRolesAsync(PosDbContext db, DateTime now, CancellationToken ct)
    {
        // The role names were expanded from QCOfficer/QCHead to the explicit
        // QualityControl names so the authorization vocabulary matches the
        // application terminology. Preserve existing assignments.
        var mappings = new[]
        {
            (Legacy: "QCOfficer", Current: RoleNames.QualityControlOfficer),
            (Legacy: "QCHead", Current: RoleNames.QualityControlHead)
        };

        foreach (var mapping in mappings)
        {
            var legacy = await db.Roles.FirstOrDefaultAsync(x => x.Name == mapping.Legacy, ct);
            var current = await db.Roles.FirstOrDefaultAsync(x => x.Name == mapping.Current, ct);
            if (legacy is null || current is null) continue;

            var assignments = await db.UserRoles.Where(x => x.RoleId == legacy.Id).ToListAsync(ct);
            foreach (var assignment in assignments)
            {
                if (await db.UserRoles.AnyAsync(x => x.UserId == assignment.UserId && x.RoleId == current.Id && x.BranchId == assignment.BranchId, ct))
                    continue;

                db.UserRoles.Add(new UserRole
                {
                    UserId = assignment.UserId,
                    RoleId = current.Id,
                    BranchId = assignment.BranchId,
                    AssignedUtc = now
                });

                var user = await db.Users.FirstOrDefaultAsync(x => x.Id == assignment.UserId, ct);
                if (user is not null && string.Equals(user.Role, mapping.Legacy, StringComparison.OrdinalIgnoreCase))
                    user.Role = mapping.Current;
            }
        }

        await db.SaveChangesAsync(ct);
    }

}
