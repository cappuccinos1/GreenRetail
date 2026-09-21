namespace GreenRetail.Refunds;

public partial class RefundsPage : ContentPage
{
    private readonly RefundsViewModel _viewModel;

    public RefundsPage(RefundsViewModel viewModel)
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
            _viewModel.StatusMessage = $"Refunds error: {ex.Message}";
        }
    }
}