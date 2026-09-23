namespace GreenRetail.Features.Catalog;

public partial class PosPage : ContentPage
{
    private readonly PosViewModel _viewModel;

    public PosPage(PosViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
        _viewModel = viewModel;

    }

    private void OnSearchCompleted(object? sender, EventArgs e)
    {
        if (_viewModel.SearchCommand.CanExecute(null))
            _viewModel.SearchCommand.Execute(null);
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