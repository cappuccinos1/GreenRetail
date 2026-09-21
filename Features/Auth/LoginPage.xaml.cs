namespace GreenRetail.Features.Auth;

public partial class LoginPage : ContentPage
{
    private readonly LoginViewModel _viewModel;

    public LoginPage(LoginViewModel viewModel)
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
            
            // Auto-focus the username field when the screen loads
            UserNameEntry?.Focus();
        }
        catch (Exception ex)
        {
            _viewModel.ErrorMessage = $"Startup error: {ex.Message}";
        }
    }

    private void OnUserNameCompleted(object? sender, EventArgs e)
    {
        // When user presses Enter on Username, jump to Password
        PasswordEntry?.Focus();
    }

    private async void OnPasswordCompleted(object? sender, EventArgs e)
    {
        // When user presses Enter on Password, trigger the Login command
        if (_viewModel.LoginCommand.CanExecute(null))
        {
            await _viewModel.LoginCommand.ExecuteAsync(null);
        }
    }
}