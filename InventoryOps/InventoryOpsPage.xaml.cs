namespace GreenRetail.InventoryOps;

public partial class InventoryOpsPage : ContentPage
{
    private readonly InventoryOpsViewModel _viewModel;

    public InventoryOpsPage(InventoryOpsViewModel viewModel)
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
            _viewModel.StatusMessage = $"Inventory ops error: {ex.Message}";
        }
    }
}