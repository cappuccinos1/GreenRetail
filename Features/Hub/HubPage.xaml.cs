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

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _viewModel.Initialize();
    }
}