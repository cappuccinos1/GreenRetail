using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GreenRetail.Shared.Navigation;
using GreenRetail.Shared.State;

namespace GreenRetail.Features.Hub;

public sealed record HubTile(string Title, string Icon, string Route, string RequiredRole);

public partial class HubViewModel : ObservableObject
{
    private readonly INavigationService _navigation;
    private readonly ICurrentUserService _currentUser;

    [ObservableProperty]
    private string userName = string.Empty;

    [ObservableProperty]
    private string role = string.Empty;

    public ObservableCollection<HubTile> Tiles { get; } = new();

    public HubViewModel(INavigationService navigation, ICurrentUserService currentUser)
    {
        _navigation = navigation;
        _currentUser = currentUser;
    }

    public void Initialize()
    {
        UserName = _currentUser.DisplayName ?? "Unknown";
        Role = _currentUser.Role ?? "None";

        Tiles.Clear();

        // Define all possible hubs
        var allTiles = new List<HubTile>
        {
            new("Sell / Till", "cart.png", "register", "Cashier"),
            new("Cash Sessions", "cash.png", "cash-sessions", "CashierManager"),
            new("Inventory", "box.png", "inventory-ops", "InventoryOfficer"),
            new("Purchasing", "truck.png", "procurement", "Buyer"),
            new("Refunds", "return.png", "refunds", "CashierManager"),
            new("Security", "shield.png", "security", "SecurityOfficer")
        };

        // Filter based on role (Simplified for MVP. Later this uses RBAC PermissionCodes)
        var allowedRoles = new List<string> { "Owner", "Manager", Role };
        
        foreach (var tile in allTiles)
        {
            if (Role == "Owner" || Role == "Manager" || Role == tile.RequiredRole)
            {
                Tiles.Add(tile);
            }
        }
    }

    [RelayCommand]
    private async Task NavigateToHub(HubTile tile)
    {
        if (tile is null) return;
        await _navigation.NavigateToAsync(tile.Route);
    }

    [RelayCommand]
    private async Task Logout()
    {
        _currentUser.Clear();
        await _navigation.GoToLoginAsync();
    }
}