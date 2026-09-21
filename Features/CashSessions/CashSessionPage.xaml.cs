namespace GreenRetail.Features.CashSessions;

public partial class CashSessionPage : ContentPage
{
    private readonly CashSessionViewModel _viewModel;

    public CashSessionPage(CashSessionViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
        _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        try
        {
            await _viewModel.InitializeAsync();
        }
        catch (Exception ex)
        {
            _viewModel.StatusMessage = $"Cash session error: {ex.Message}";
        }
    }
}