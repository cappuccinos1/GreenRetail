namespace GreenRetail.Features.Hubs;
public partial class InventoryHubPage : ContentPage
{
    public InventoryHubPage() => InitializeComponent();
    private async void OnInventoryClicked(object s, EventArgs e) => await Shell.Current.GoToAsync("inventory-ops");
    private async void OnPurchasingClicked(object s, EventArgs e) => await Shell.Current.GoToAsync("procurement");
}
