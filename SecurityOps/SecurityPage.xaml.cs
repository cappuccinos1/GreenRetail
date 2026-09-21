namespace GreenRetail.SecurityOps;

public partial class SecurityPage : ContentPage
{
    private readonly SecurityViewModel _viewModel;

    public SecurityPage(SecurityViewModel viewModel)
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
            _viewModel.StatusMessage = $"Security error: {ex.Message}";
        }
    }
}