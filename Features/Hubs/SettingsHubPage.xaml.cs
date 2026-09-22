namespace GreenRetail.Features.Hubs;
public partial class SettingsHubPage : ContentPage
{
    public SettingsHubPage() => InitializeComponent();
    private async void OnSetupClicked(object s, EventArgs e) => await Shell.Current.GoToAsync("system-setup");
    private async void OnRbacClicked(object s, EventArgs e) => await Shell.Current.GoToAsync("rbac-explorer");
    private async void OnPasswordClicked(object s, EventArgs e) => await Shell.Current.GoToAsync("change-password");
}
