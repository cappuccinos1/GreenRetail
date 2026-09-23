namespace GreenRetail.Features.Hubs;
public partial class AuditHubPage : ContentPage
{
    public AuditHubPage() => InitializeComponent();
    private async void OnSecurityClicked(object s, EventArgs e) => await Shell.Current.GoToAsync("security");
    private async void OnRbacClicked(object s, EventArgs e) => await Shell.Current.GoToAsync("rbac-explorer");
}
