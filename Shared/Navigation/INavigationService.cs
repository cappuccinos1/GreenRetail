namespace GreenRetail.Shared.Navigation;

public interface INavigationService
{
    Task GoToLoginAsync();
    Task GoToDashboardAsync(); // NEW
    Task GoToHomeAsync();
    Task GoToCheckoutAsync();
    Task GoToCashSessionAsync();
    Task GoToReportsAsync();
    Task GoBackAsync();

    Task GoToInventoryOpsAsync();
    Task GoToProcurementAsync();
    Task GoToRefundsAsync();
    Task GoToSecurityAsync();
    Task GoToTrialBalanceAsync();
    Task GoToRbacExplorerAsync();

    Task NavigateToAsync(string route);
}