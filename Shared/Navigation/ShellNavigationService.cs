namespace GreenRetail.Shared.Navigation;

public sealed class ShellNavigationService : INavigationService
{
    public Task GoToLoginAsync()
        => NavigateAsync("//login");

    public Task GoToDashboardAsync() // NEW
        => NavigateAsync("//dashboard");

    public Task GoToHomeAsync()
        => NavigateAsync("//dashboard"); // Home now means Dashboard

    public Task GoToCheckoutAsync()
        => NavigateAsync("register");

    public Task GoToCashSessionAsync()
        => NavigateAsync("cash-sessions");

    public Task GoToReportsAsync()
        => NavigateAsync("trial-balance");

    public Task GoBackAsync()
        => NavigateAsync("..");

    public Task GoToInventoryOpsAsync()
        => NavigateAsync("inventory-ops");

    public Task GoToProcurementAsync()
        => NavigateAsync("procurement");

    public Task GoToRefundsAsync()
        => NavigateAsync("refunds");

    public Task GoToSecurityAsync()
        => NavigateAsync("security");

    public Task GoToTrialBalanceAsync()
        => NavigateAsync("trial-balance");

    public Task GoToRbacExplorerAsync()
        => NavigateAsync("rbac-explorer");

    public Task NavigateToAsync(string route)
    {
        if (string.IsNullOrWhiteSpace(route))
            return Task.CompletedTask;

        return Shell.Current?.GoToAsync(route) ?? Task.CompletedTask;
    }

    private static Task NavigateAsync(string route)
    {
        return Shell.Current?.GoToAsync(route) ?? Task.CompletedTask;
    }
}