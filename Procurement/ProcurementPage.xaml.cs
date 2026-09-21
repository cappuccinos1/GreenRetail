namespace GreenRetail.Procurement;

public partial class ProcurementPage : ContentPage
{
    private readonly ProcurementViewModel _viewModel;

    public ProcurementPage(ProcurementViewModel viewModel)
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
            _viewModel.StatusMessage = $"Procurement error: {ex.Message}";
        }
    }
}