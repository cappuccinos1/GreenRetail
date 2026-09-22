namespace GreenRetail.Features.Hub;

public partial class HubPage : ContentPage
{
    private readonly HubViewModel _viewModel;

    public HubPage(HubViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
        _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.InitializeAsync();
    }
}