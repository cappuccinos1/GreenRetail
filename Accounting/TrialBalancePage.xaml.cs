namespace GreenRetail.Accounting;

public partial class TrialBalancePage : ContentPage
{
    private readonly TrialBalanceViewModel _viewModel;

    public TrialBalancePage(TrialBalanceViewModel viewModel)
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
            _viewModel.StatusMessage = $"Trial balance error: {ex.Message}";
        }
    }
}