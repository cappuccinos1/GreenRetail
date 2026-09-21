using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GreenRetail.Data;
using GreenRetail.Shared.Navigation;
using GreenRetail.Shared.State;

namespace GreenRetail.Features.Auth;

public partial class LoginViewModel : ObservableObject
{
    private readonly IStartupService _startup;
    private readonly IAuthenticateUserUseCase _authenticateUser;
    private readonly ICurrentUserService _currentUser;
    private readonly INavigationService _navigation;

    [ObservableProperty]
    private string userName = string.Empty;

    [ObservableProperty]
    private string password = string.Empty;

    [ObservableProperty]
    private string errorMessage = string.Empty;

    [ObservableProperty]
    private bool isBusy;

    public LoginViewModel(
        IStartupService startup,
        IAuthenticateUserUseCase authenticateUser,
        ICurrentUserService currentUser,
        INavigationService navigation)
    {
        _startup = startup;
        _authenticateUser = authenticateUser;
        _currentUser = currentUser;
        _navigation = navigation;
    }

    public async Task InitializeAsync()
    {
        await _startup.InitializeAsync();
    }

    [RelayCommand]
    private async Task LoginAsync()
    {
        if (IsBusy)
            return;

        try
        {
            IsBusy = true;
            ErrorMessage = string.Empty;

            var result = await _authenticateUser.ExecuteAsync(
                new AuthenticateUserCommand(UserName, Password));

            if (!result.IsSuccess)
            {
                ErrorMessage = result.Error ?? "Login failed.";
                return;
            }

            var user = result.Value;

            _currentUser.SetUser(
                user.Id,
                user.UserName,
                user.DisplayName,
                user.Role);

            UserName = string.Empty;
            Password = string.Empty;

            await _navigation.GoToDashboardAsync();
        }
        catch (Exception ex)
        {
            // Catch hidden DB/System errors and show them to the user
            ErrorMessage = $"System error: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }
}