namespace GreenRetail.Features.Hubs;
public partial class SellHubPage : ContentPage
{
    public SellHubPage() => InitializeComponent();
    private Task Go(string route) => Shell.Current.GoToAsync(route);
    private async void OnRegisterClicked(object s, EventArgs e) => await Go("register");
    private async void OnCashClicked(object s, EventArgs e) => await Go("cash-sessions");
    private async void OnRefundsClicked(object s, EventArgs e) => await Go("refunds");
    private async void OnSecurityClicked(object s, EventArgs e) => await Go("security");
}
