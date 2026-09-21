using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GreenRetail.Shared.Navigation;
using GreenRetail.Shared.State;

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

    [ObservableProperty]
    private string userName = string.Empty;

    [ObservableProperty]
    private string role = string.Empty;

    [ObservableProperty]
    private string greeting = string.Empty;

    public ObservableCollection<DashboardTile> Tiles { get; } = new();

    public DashboardViewModel(INavigationService navigation, ICurrentUserService currentUser)
    {
        _navigation = navigation;
        _currentUser = currentUser;
    }

    public void Initialize()
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
            new("People & Admin", "HR requests, users, and system settings", "settings.png", "rbac-explorer", "#64748B")
        };

        // Filter tiles based on role (Simplified MVP mapping)
        // Owner and Manager see everything.
        if (Role == "Owner" || Role == "Manager")
        {
            foreach (var tile in allTiles) Tiles.Add(tile);
        }
        else if (Role == "Cashier")
        {
            Tiles.Add(allTiles[0]); // Register only
        }
        else if (Role == "CashierManager")
        {
            Tiles.Add(allTiles[0]); // Register
            Tiles.Add(allTiles[1]); // Till Management
            Tiles.Add(allTiles[7]); // People & Admin (for refunds/overrides)
        }
        else if (Role == "InventoryOfficer" || Role == "InventoryHead")
        {
            Tiles.Add(allTiles[2]); // Inventory Ops
        }
        else if (Role == "Buyer" || Role == "ProcurementHead")
        {
            Tiles.Add(allTiles[3]); // Purchasing
        }
        else if (Role == "QCOfficer" || Role == "QCHead")
        {
            Tiles.Add(allTiles[4]); // Quality Control
        }
        else if (Role == "AccountsOfficer" || Role == "AccountsHead")
        {
            Tiles.Add(allTiles[5]); // Accounts
        }
        else if (Role == "AuditOfficer" || Role == "AuditHead" || Role == "SecurityOfficer")
        {
            Tiles.Add(allTiles[6]); // Audit & Security
        }
        else if (Role == "HROfficer" || Role == "HRHead" || Role == "ITAdmin")
        {
            Tiles.Add(allTiles[7]); // People & Admin
        }
        else
        {
            // Fallback: show everything if role is unknown (dev mode)
            foreach (var tile in allTiles) Tiles.Add(tile);
        }
    }

    [RelayCommand]
    private async Task NavigateToWorkspace(DashboardTile tile)
    {
        if (tile is null) return;
        await _navigation.NavigateToAsync(tile.Route);
    }
}