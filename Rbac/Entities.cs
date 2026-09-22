using GreenRetail.Data.Entities;

namespace GreenRetail.Rbac;

public static class PermissionCodes
{
    // POS / till
    public const string PosSaleCreate = "pos.sale.create";
    public const string PosSaleSuspend = "pos.sale.suspend";
    public const string PosSessionOpen = "pos.session.open";
    public const string PosSessionClose = "pos.session.close";
    public const string PosRefundInitiate = "pos.refund.initiate";
    public const string PosRefundApprove = "pos.refund.approve";
    public const string PosRefundHighValueApprove = "pos.refund.high_value.approve";
    public const string PosReceiptReprint = "pos.receipt.reprint";
    public const string PosStockOverrideRequest = "pos.stock_override.request";
    public const string PosStockOverrideResolve = "pos.stock_override.resolve";

    // Inventory / catalogue / pricing
    public const string InventoryProductManage = "inventory.product.manage";
    public const string InventoryStockView = "inventory.stock.view";
    public const string InventoryStockAdjustRequest = "inventory.stock.adjust.request";
    public const string InventoryStockAdjustApprove = "inventory.stock.adjust.approve";
    public const string InventoryStockOverrideResolve = "inventory.stock.override.resolve";
    public const string InventorySupplierManage = "inventory.supplier.manage";
    public const string InventoryTransferCreate = "inventory.transfer.create";
    public const string InventoryTransferApprove = "inventory.transfer.approve";
    public const string InventoryNegativeStockAllow = "inventory.stock.negative.allow";
    public const string InventoryQuarantineManage = "inventory.quarantine.manage";

    // Purchasing
    public const string PurchasingPoCreate = "purchasing.po.create";
    public const string PurchasingPoView = "purchasing.po.view";

    // Receiving / QC. QC never writes stock directly.
    public const string ReceivingGrnPost = "receiving.grn.post";
    public const string ReceivingInvoiceWithoutPo = "receiving.invoice_without_po";
    public const string ReceivingNoPoConfirm = "receiving.no_po.confirm";
    public const string QcInspect = "qc.inspect";
    public const string QcApprove = "qc.approve";

    // Cash / accounts
    public const string CashCount = "cash.count";
    public const string CashVarianceApprove = "cash.variance.approve";
    public const string CashDepositRecord = "cash.deposit.record";
    public const string FinanceView = "finance.view";
    public const string FinanceStatementPrepare = "finance.statement.prepare";
    public const string FinanceOrganisationStatementPrepare = "finance.organization_statement.prepare";

    // Reports deliberately separate performance-only views from sales figures.
    public const string ReportsCashierPerformanceView = "reports.cashier_performance.view";
    public const string ReportsSalesView = "reports.sales.view";

    // Audit / compliance merged by design.
    public const string AuditView = "audit.view";
    public const string AuditCaseManage = "audit.case.manage";
    public const string AuditRecommend = "audit.recommend";

    // HR / IT user lifecycle
    public const string HrUserRequest = "hr.user.request";
    public const string HrRoleAssign = "hr.role.assign";
    public const string HrRoleChangeRequest = "hr.role_change.request";
    public const string ItUserCreate = "it.user.create";
    public const string ItSystemManage = "it.system.manage";
    public const string ItRoleImplement = "it.role.implement";

    // Security
    public const string SecurityReceiptVerify = "security.receipt.verify";
    public const string SecurityIncidentCreate = "security.incident.create";

    // Store credit
    public const string StoreCreditIssue = "storecredit.issue";
    public const string StoreCreditRedeem = "storecredit.redeem";

    public static readonly string[] All =
    {
        PosSaleCreate, PosSaleSuspend, PosSessionOpen, PosSessionClose,
        PosRefundInitiate, PosRefundApprove, PosRefundHighValueApprove, PosReceiptReprint,
        PosStockOverrideRequest, PosStockOverrideResolve,
        InventoryProductManage, InventoryStockView, InventoryStockAdjustRequest,
        InventoryStockAdjustApprove, InventoryStockOverrideResolve,
        InventorySupplierManage, InventoryTransferCreate, InventoryTransferApprove,
        InventoryNegativeStockAllow, InventoryQuarantineManage,
        PurchasingPoCreate, PurchasingPoView,
        ReceivingGrnPost, ReceivingInvoiceWithoutPo, ReceivingNoPoConfirm,
        QcInspect, QcApprove,
        CashCount, CashVarianceApprove, CashDepositRecord,
        FinanceView, FinanceStatementPrepare, FinanceOrganisationStatementPrepare,
        ReportsCashierPerformanceView, ReportsSalesView,
        AuditView, AuditCaseManage, AuditRecommend,
        HrUserRequest, HrRoleAssign, HrRoleChangeRequest,
        ItUserCreate, ItSystemManage, ItRoleImplement,
        SecurityReceiptVerify, SecurityIncidentCreate,
        StoreCreditIssue, StoreCreditRedeem
    };
}

public static class RoleNames
{
    public const string Owner = "Owner";
    public const string ITHead = "ITHead";
    public const string HROfficer = "HROfficer";
    public const string HRHead = "HRHead";
    public const string BranchManager = "BranchManager";
    public const string Cashier = "Cashier";
    public const string CashierManager = "CashierManager";
    public const string InventoryOfficer = "InventoryOfficer";
    public const string InventoryHead = "InventoryHead";
    public const string Buyer = "Buyer";
    public const string ProcurementHead = "ProcurementHead";
    public const string QCOfficer = "QCOfficer";
    public const string QCHead = "QCHead";
    public const string AccountsOfficer = "AccountsOfficer";
    public const string AccountsHead = "AccountsHead";
    public const string AccountsGroupHead = "AccountsGroupHead";
    public const string AuditOfficer = "AuditOfficer";
    public const string AuditHead = "AuditHead";
    public const string SecurityOfficer = "SecurityOfficer";

    public static readonly HashSet<string> GlobalScopeRoles = new(StringComparer.OrdinalIgnoreCase)
    {
        Owner, ITHead, HROfficer, HRHead, AuditOfficer, AuditHead
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
