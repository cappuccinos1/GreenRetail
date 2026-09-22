using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GreenRetail.Shared.Navigation;
using GreenRetail.Shared.State;
using GreenRetail.Rbac;

namespace GreenRetail.Features.Hub;

public sealed record HubTile(string Title, string Icon, string Route);

public partial class HubViewModel : ObservableObject
{
    private readonly INavigationService _navigation;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuthorizationService _authorization;

    [ObservableProperty]
    private string userName = string.Empty;

    [ObservableProperty]
    private string role = string.Empty;

    public ObservableCollection<HubTile> Tiles { get; } = new();

    public HubViewModel(INavigationService navigation, ICurrentUserService currentUser, IAuthorizationService authorization)
    {
        _navigation = navigation;
        _currentUser = currentUser;
        _authorization = authorization;
    }

    public async Task InitializeAsync()
    {
        UserName = _currentUser.DisplayName ?? "Unknown";
        Role = _currentUser.Role ?? "None";

        Tiles.Clear();

        // Define all possible hubs
        var allTiles = new List<HubTile>
        {
            new("Sell / Till", "cart.png", "register"),
            new("Cash Sessions", "cash.png", "cash-sessions"),
            new("Inventory", "box.png", "inventory-ops"),
            new("Purchasing", "truck.png", "procurement"),
            new("Refunds", "return.png", "refunds"),
            new("Security", "shield.png", "security")
        };

        if (!_currentUser.UserId.HasValue) return;

        var userId = _currentUser.UserId.Value;
        var permissions = new[]
        {
            new[] { PermissionCodes.PosSaleCreate },
            new[] { PermissionCodes.PosSessionOpen, PermissionCodes.PosSessionClose },
            new[] { PermissionCodes.InventoryProductManage, PermissionCodes.InventoryStockView },
            new[] { PermissionCodes.PurchasingPoCreate, PermissionCodes.PurchasingPoView },
            new[] { PermissionCodes.PosRefundInitiate, PermissionCodes.PosRefundApprove },
            new[] { PermissionCodes.SecurityReceiptVerify, PermissionCodes.SecurityIncidentCreate }
        };

        for (var i = 0; i < allTiles.Count; i++)
        {
            if (await _authorization.HasAnyPermissionAsync(userId, permissions[i], cancellationToken: CancellationToken.None))
                Tiles.Add(allTiles[i]);
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