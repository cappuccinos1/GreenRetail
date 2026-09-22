using GreenRetail.Accounting;
using GreenRetail.Features.CashSessions;
using GreenRetail.Features.Auth;
using GreenRetail.Features.Dashboard;
using GreenRetail.Features.Hubs;
using GreenRetail.Features.Register;
using GreenRetail.InventoryOps;
using GreenRetail.Procurement;
using GreenRetail.Rbac;
using GreenRetail.Refunds;
using GreenRetail.SecurityOps;
using GreenRetail.Features.Setup;

namespace GreenRetail;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        // Dashboard
        Routing.RegisterRoute("dashboard", typeof(DashboardPage));

        // Core Workspaces
        Routing.RegisterRoute("register", typeof(RegisterPage));
        Routing.RegisterRoute("cash-sessions", typeof(CashSessionPage));
        Routing.RegisterRoute("change-password", typeof(ChangePasswordPage));

        // Back Office Workspaces
        Routing.RegisterRoute("inventory-ops", typeof(InventoryOpsPage));
        Routing.RegisterRoute("procurement", typeof(ProcurementPage));
        Routing.RegisterRoute("refunds", typeof(RefundsPage));
        Routing.RegisterRoute("security", typeof(SecurityPage));

        Routing.RegisterRoute("trial-balance", typeof(TrialBalancePage));
        Routing.RegisterRoute("rbac-explorer", typeof(RbacExplorerPage));
        Routing.RegisterRoute("system-setup", typeof(SetupPage));
    }
}