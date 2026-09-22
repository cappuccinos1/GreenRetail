namespace GreenRetail.Features.Hubs;
public partial class PurchasingHubPage : ContentPage
{
    public PurchasingHubPage() => InitializeComponent();
    private async void OnProcurementClicked(object s, EventArgs e) => await Shell.Current.GoToAsync("procurement");
    private async void OnQcClicked(object s, EventArgs e) => await Shell.Current.GoToAsync("qc");
}
