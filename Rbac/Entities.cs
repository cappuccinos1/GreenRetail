using GreenRetail.Data.Entities;

namespace GreenRetail.Rbac;

public static class PermissionCodes
{
    public const string PosSaleCreate = "pos.sale.create";
    public const string PosSaleSuspend = "pos.sale.suspend";
    public const string PosSessionOpen = "pos.session.open";
    public const string PosSessionClose = "pos.session.close";
    public const string PosRefundInitiate = "pos.refund.initiate";
    public const string PosRefundApprove = "pos.refund.approve";
    public const string PosReceiptReprint = "pos.receipt.reprint";
    public const string PosStockOverrideRequest = "pos.stock_override.request";

    public const string InventoryProductManage = "inventory.product.manage";
    public const string InventoryStockView = "inventory.stock.view";
    public const string InventoryStockAdjustRequest = "inventory.stock.adjust.request";
    public const string InventoryStockAdjustApprove = "inventory.stock.adjust.approve";
    public const string InventoryStockOverrideResolve = "inventory.stock.override.resolve";
    public const string InventorySupplierManage = "inventory.supplier.manage";

    public const string PurchasingPoCreate = "purchasing.po.create";
    public const string PurchasingPoView = "purchasing.po.view";

    public const string ReceivingGrnPost = "receiving.grn.post";

    public const string QcInspect = "qc.inspect";
    public const string QcApprove = "qc.approve";

    public const string CashCount = "cash.count";
    public const string CashVarianceApprove = "cash.variance.approve";
    public const string CashDepositRecord = "cash.deposit.record";

    public const string FinanceView = "finance.view";
    public const string FinanceStatementPrepare = "finance.statement.prepare";

    public const string AuditView = "audit.view";
    public const string AuditCaseManage = "audit.case.manage";

    public const string HrUserRequest = "hr.user.request";
    public const string HrRoleAssign = "hr.role.assign";

    public const string ItUserCreate = "it.user.create";
    public const string ItSystemManage = "it.system.manage";

    public const string SecurityReceiptVerify = "security.receipt.verify";
    public const string SecurityIncidentCreate = "security.incident.create";

    public const string StoreCreditIssue = "storecredit.issue";
    public const string StoreCreditRedeem = "storecredit.redeem";

    public static readonly string[] All =
    {
        PosSaleCreate,
        PosSaleSuspend,
        PosSessionOpen,
        PosSessionClose,
        PosRefundInitiate,
        PosRefundApprove,
        PosReceiptReprint,
        PosStockOverrideRequest,

        InventoryProductManage,
        InventoryStockView,
        InventoryStockAdjustRequest,
        InventoryStockAdjustApprove,
        InventoryStockOverrideResolve,
        InventorySupplierManage,

        PurchasingPoCreate,
        PurchasingPoView,

        ReceivingGrnPost,

        QcInspect,
        QcApprove,

        CashCount,
        CashVarianceApprove,
        CashDepositRecord,

        FinanceView,
        FinanceStatementPrepare,

        AuditView,
        AuditCaseManage,

        HrUserRequest,
        HrRoleAssign,

        ItUserCreate,
        ItSystemManage,

        SecurityReceiptVerify,
        SecurityIncidentCreate,

        StoreCreditIssue,
        StoreCreditRedeem
    };
}

public class Role
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    public bool IsSystem { get; set; }

    public DateTime CreatedUtc { get; set; }
}

public class Permission
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Code { get; set; } = string.Empty;
    public string Module { get; set; } = string.Empty;
    public string? Description { get; set; }

    public DateTime CreatedUtc { get; set; }
}

public class RolePermission
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid RoleId { get; set; }
    public Role? Role { get; set; }

    public Guid PermissionId { get; set; }
    public Permission? Permission { get; set; }
}

public class UserRole
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid UserId { get; set; }
    public AppUser? User { get; set; }

    public Guid RoleId { get; set; }
    public Role? Role { get; set; }

    public Guid? BranchId { get; set; }
    public Branch? Branch { get; set; }

    public DateTime AssignedUtc { get; set; }
}

public class PermissionOverride
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid UserId { get; set; }
    public AppUser? User { get; set; }

    public Guid PermissionId { get; set; }
    public Permission? Permission { get; set; }

    public Guid? BranchId { get; set; }

    public Guid GrantedById { get; set; }
    public AppUser? GrantedBy { get; set; }

    public string Reason { get; set; } = string.Empty;

    public DateTime StartUtc { get; set; }
    public DateTime EndUtc { get; set; }

    public DateTime CreatedUtc { get; set; }
}