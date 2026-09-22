namespace GreenRetail.Features.Hubs;
public partial class AccountsHubPage : ContentPage
{
    public AccountsHubPage() => InitializeComponent();
    private async void OnTrialBalanceClicked(object s, EventArgs e) => await Shell.Current.GoToAsync("trial-balance");
    private async void OnReportsClicked(object s, EventArgs e) => await Shell.Current.GoToAsync("trial-balance");
    private async void OnCashClicked(object s, EventArgs e) => await Shell.Current.GoToAsync("cash-sessions");
}
