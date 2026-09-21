using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GreenRetail.Shared.State;

namespace GreenRetail.Rbac;

public partial class RbacExplorerViewModel : ObservableObject
{
    private readonly IGetRbacExplorerDataQuery _getRbacData;
    private readonly ICurrentUserService _currentUser;

    [ObservableProperty]
    private string statusMessage = string.Empty;

    public ObservableCollection<RoleListItem> Roles { get; } = new();
    public ObservableCollection<PermissionListItem> Permissions { get; } = new();
    public ObservableCollection<UserPermissionCheckItem> UserPermissions { get; } = new();

    public RbacExplorerViewModel(
        IGetRbacExplorerDataQuery getRbacData,
        ICurrentUserService currentUser)
    {
        _getRbacData = getRbacData;
        _currentUser = currentUser;
    }

    public async Task InitializeAsync()
    {
        await RefreshAsync();
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        if (_currentUser.UserId is null)
        {
            StatusMessage = "No authenticated user.";
            return;
        }

        var result = await _getRbacData.ExecuteAsync(new GetRbacExplorerDataQueryRequest(_currentUser.UserId.Value));

        if (!result.IsSuccess)
        {
            StatusMessage = result.Error ?? "Could not load RBAC data.";
            return;
        }

        var data = result.Value;

        Roles.Clear();
        Permissions.Clear();
        UserPermissions.Clear();

        foreach (var role in data.Roles)
        {
            Roles.Add(role);
        }

        foreach (var permission in data.Permissions)
        {
            Permissions.Add(permission);
        }

        foreach (var userPermission in data.CurrentUserPermissions)
        {
            UserPermissions.Add(userPermission);
        }

        StatusMessage = $"RBAC data loaded for {_currentUser.DisplayName}.";
    }
}