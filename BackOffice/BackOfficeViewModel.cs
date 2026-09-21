using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GreenRetail.Shared.Navigation;

namespace GreenRetail.BackOffice;

public partial class BackOfficeViewModel : ObservableObject
{
    private readonly INavigationService _navigation;

    public BackOfficeViewModel(INavigationService navigation)
    {
        _navigation = navigation;
    }

    [RelayCommand]
    private Task GoInventoryOpsAsync()
        => _navigation.GoToInventoryOpsAsync();

    [RelayCommand]
    private Task GoProcurementAsync()
        => _navigation.GoToProcurementAsync();

    [RelayCommand]
    private Task GoRefundsAsync()
        => _navigation.GoToRefundsAsync();

    [RelayCommand]
    private Task GoSecurityAsync()
        => _navigation.GoToSecurityAsync();

    [RelayCommand]
    private Task GoTrialBalanceAsync()
        => _navigation.GoToTrialBalanceAsync();

    [RelayCommand]
    private Task GoRbacExplorerAsync()
        => _navigation.GoToRbacExplorerAsync();
}