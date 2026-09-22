namespace GreenRetail.Features.Hubs;
public partial class QcHubPage : ContentPage
{
    public QcHubPage() => InitializeComponent();
    private async void OnReceivingClicked(object s, EventArgs e) => await Shell.Current.GoToAsync("procurement");
}
