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
using GreenRetail.Shared.State;

namespace GreenRetail;

public partial class AppShell : Shell
{
    private bool _redirecting;

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
        Routing.RegisterRoute("quality-control-workflow", typeof(QualityControlPage));
        Routing.RegisterRoute("refunds", typeof(RefundsPage));
        Routing.RegisterRoute("security", typeof(SecurityPage));

        Routing.RegisterRoute("trial-balance", typeof(TrialBalancePage));
        Routing.RegisterRoute("rbac-explorer", typeof(RbacExplorerPage));
        Routing.RegisterRoute("system-setup", typeof(SetupPage));

        Dispatcher.Dispatch(async () =>
        {
            if (Application.Current?.Handler?.MauiContext?.Services.GetService(typeof(ICurrentUserService)) is ICurrentUserService currentUser
                && !currentUser.IsAuthenticated)
            {
                await GoToAsync("//login");
            }
        });
    }

    private async void OnShellNavigated(object? sender, ShellNavigatedEventArgs e)
    {
        if (_redirecting) return;

        var currentUser = Application.Current?.Handler?.MauiContext?.Services.GetService(typeof(ICurrentUserService)) as ICurrentUserService;
        if (currentUser is null || currentUser.IsAuthenticated) return;

        var location = e.Current?.Location?.OriginalString ?? string.Empty;
        if (location.Contains("/login", StringComparison.OrdinalIgnoreCase)) return;

        _redirecting = true;
        try { await GoToAsync("//login"); }
        finally { _redirecting = false; }
    }
}