namespace GreenRetail.Procurement;

public partial class QualityControlPage : ContentPage
{
    private readonly QualityControlViewModel _viewModel;

    public QualityControlPage(QualityControlViewModel viewModel)
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
