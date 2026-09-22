using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GreenRetail.Shared.Navigation;
using GreenRetail.Shared.State;
using GreenRetail.Rbac;

namespace GreenRetail.Features.Dashboard;

public sealed record DashboardTile(
    string Title,
    string Description,
    string Icon,
    string Route,
    string BackgroundColor);

public partial class DashboardViewModel : ObservableObject
{
    private readonly INavigationService _navigation;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuthorizationService _authorization;

    [ObservableProperty]
    private string userName = string.Empty;

    [ObservableProperty]
    private string role = string.Empty;

    [ObservableProperty]
    private string greeting = string.Empty;

    public ObservableCollection<DashboardTile> Tiles { get; } = new();

    public DashboardViewModel(INavigationService navigation, ICurrentUserService currentUser, IAuthorizationService authorization)
    {
        _navigation = navigation;
        _currentUser = currentUser;
        _authorization = authorization;
    }

    public async Task InitializeAsync()
    {
        UserName = _currentUser.DisplayName ?? "User";
        Role = _currentUser.Role ?? "None";
        Greeting = $"Welcome back, {UserName}";

        Tiles.Clear();

        // Define all possible workspace tiles
        var allTiles = new List<DashboardTile>
        {
            new("Cashier Register", "Process sales and scan items", "cart.png", "register", "#10B981"),
            new("Till Management", "Open/close shifts and manage cashiers", "cash.png", "cash-sessions", "#3B82F6"),
            new("Inventory Ops", "Manage stock, catalogs, and adjustments", "box.png", "inventory-ops", "#8B5CF6"),
            new("Purchasing", "Raise POs and manage suppliers", "truck.png", "procurement", "#F59E0B"),
            new("Quality Control", "Inspect incoming goods and GRNs", "shield.png", "qc", "#EF4444"),
            new("Accounts", "Daybook, expenses, and reconciliations", "calculator.png", "trial-balance", "#06B6D4"),
            new("Audit & Security", "Logs, corrections, and receipt verification", "search.png", "security", "#6366F1"),
            new("People & Admin", "HR requests, users, and system settings", "settings.png", "rbac-explorer", "#64748B"),
            new("System Setup", "Create branches and POS terminals", "settings.png", "system-setup", "#0F766E")
        };

        if (!_currentUser.UserId.HasValue) return;

        var userId = _currentUser.UserId.Value;
        var allowed = new[]
        {
            new[] { PermissionCodes.PosSaleCreate },
            new[] { PermissionCodes.PosSessionOpen, PermissionCodes.PosSessionClose },
            new[] { PermissionCodes.InventoryProductManage, PermissionCodes.InventoryStockView },
            new[] { PermissionCodes.PurchasingPoCreate, PermissionCodes.PurchasingPoView },
            new[] { PermissionCodes.QcInspect, PermissionCodes.QcApprove },
            new[] { PermissionCodes.FinanceView, PermissionCodes.FinanceStatementPrepare },
            new[] { PermissionCodes.AuditView, PermissionCodes.SecurityReceiptVerify, PermissionCodes.SecurityIncidentCreate },
            new[] { PermissionCodes.ItUserCreate, PermissionCodes.HrUserRequest, PermissionCodes.HrRoleAssign, PermissionCodes.ItSystemManage },
            new[] { PermissionCodes.ItSystemManage }
        };

        for (var i = 0; i < allTiles.Count; i++)
        {
            if (await _authorization.HasAnyPermissionAsync(userId, allowed[i], cancellationToken: CancellationToken.None))
                Tiles.Add(allTiles[i]);
        }
    }

    [RelayCommand]
    private async Task NavigateToWorkspace(DashboardTile tile)
    {
        if (tile is null) return;
        await _navigation.NavigateToAsync(tile.Route);
    }
}