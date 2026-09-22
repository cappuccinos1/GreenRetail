namespace GreenRetail.Features.Hubs;

public partial class SellHubPage : ContentPage
{
    public SellHubPage() => InitializeComponent();

    private async void OnActionTapped(object? sender, TappedEventArgs e)
    {
        if (e.Parameter is not string route || string.IsNullOrWhiteSpace(route))
            return;

        try
        {
            await Shell.Current.GoToAsync(route);
        }
        catch (Exception ex)
        {
            App.LogCrash(ex);
            await DisplayAlert("Navigation error", $"The requested workspace could not be opened.\n\n{ex.Message}", "OK");
        }
    }
}
