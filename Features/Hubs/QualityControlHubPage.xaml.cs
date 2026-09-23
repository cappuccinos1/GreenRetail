namespace GreenRetail.Features.Hubs;

public partial class QualityControlHubPage : ContentPage
{
    public QualityControlHubPage() => InitializeComponent();

    private async void OnReceivingClicked(object? sender, EventArgs e)
        => await Shell.Current.GoToAsync("quality-control-workflow");
}
