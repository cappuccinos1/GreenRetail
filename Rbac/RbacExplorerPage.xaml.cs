namespace GreenRetail.Rbac;

public partial class RbacExplorerPage : ContentPage
{
    private readonly RbacExplorerViewModel _viewModel;

    public RbacExplorerPage(RbacExplorerViewModel viewModel)
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
            _viewModel.StatusMessage = $"RBAC error: {ex.Message}";
        }
    }
}