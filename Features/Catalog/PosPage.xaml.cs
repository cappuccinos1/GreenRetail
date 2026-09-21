namespace GreenRetail.Features.Catalog;

public partial class PosPage : ContentPage
{
    private readonly PosViewModel _viewModel;

    public PosPage(PosViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
        _viewModel = viewModel;

        ToolbarItems.Add(new ToolbarItem
        {
            Text = "Refresh",
            Command = _viewModel.SearchCommand,
            Order = ToolbarItemOrder.Primary
        });

        ToolbarItems.Add(new ToolbarItem
        {
            Text = "Logout",
            Command = _viewModel.LogoutCommand,
            Order = ToolbarItemOrder.Primary
        });
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
            _viewModel.StatusMessage = $"Startup error: {ex.Message}";
        }
    }
}