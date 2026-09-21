namespace GreenRetail.Features.Checkout;

public partial class CheckoutPage : ContentPage
{
    private readonly CheckoutViewModel _viewModel;

    public CheckoutPage(CheckoutViewModel viewModel)
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