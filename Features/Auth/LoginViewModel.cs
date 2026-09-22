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
    private readonly IOwnerRecoveryUseCase _ownerRecovery;

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
        INavigationService navigation,
        IOwnerRecoveryUseCase ownerRecovery)
    {
        _startup = startup;
        _authenticateUser = authenticateUser;
        _currentUser = currentUser;
        _navigation = navigation;
        _ownerRecovery = ownerRecovery;
    }

    public async Task InitializeAsync()
    {
        await _startup.InitializeAsync();
    }

    public bool IsDevelopmentRecoveryEnabled
        => string.Equals(Environment.GetEnvironmentVariable("GREENRETAIL_DEV_SEED"), "true", StringComparison.OrdinalIgnoreCase);

    [RelayCommand]
    private async Task ResetOwnerAccessAsync()
    {
        if (IsBusy) return;
        try
        {
            IsBusy = true;
            ErrorMessage = string.Empty;
            await _startup.InitializeAsync();
            var result = await _ownerRecovery.ResetOwnerAccessAsync();
            if (!result.IsSuccess)
            {
                ErrorMessage = result.Error ?? "Owner recovery failed.";
                return;
            }

            UserName = result.Value.UserName;
            Password = result.Value.TemporaryPassword;
            ErrorMessage = "Owner access reset. Sign in with the temporary password shown in the password field, then change it.";
        }
        catch (Exception ex)
        {
            App.LogCrash(ex);
            ErrorMessage = "Owner recovery failed. Check the diagnostics log.";
        }
        finally { IsBusy = false; }
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

            // Startup is idempotent. Retrying here makes a transient/legacy database
            // repair recoverable without requiring the user to restart the app.
            await _startup.InitializeAsync();

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

            if (user.RequiresPasswordChange)
                await _navigation.NavigateToAsync("change-password");
            else
                await _navigation.GoToDashboardAsync();
        }
        catch (Exception ex)
        {
            // Catch hidden DB/System errors and show them to the user
            App.LogCrash(ex);
            ErrorMessage = "GreenRetail could not initialize its local database. The error was logged; close and reopen the app if retrying does not resolve it.";
        }
        finally
        {
            IsBusy = false;
        }
    }
}