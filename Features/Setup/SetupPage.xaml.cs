namespace GreenRetail.Features.Setup;

public partial class SetupPage : ContentPage
{
    private readonly SetupViewModel _viewModel;

    public SetupPage(SetupViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.InitializeAsync();
    }
}
