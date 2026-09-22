using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GreenRetail.Shared.Navigation;

namespace GreenRetail.Features.Auth;

public partial class ChangePasswordViewModel : ObservableObject
{
    private readonly IChangePasswordUseCase _changePassword;
    private readonly INavigationService _navigation;

    [ObservableProperty] private string currentPassword = string.Empty;
    [ObservableProperty] private string newPassword = string.Empty;
    [ObservableProperty] private string confirmPassword = string.Empty;
    [ObservableProperty] private string errorMessage = string.Empty;
    [ObservableProperty] private bool isBusy;

    public ChangePasswordViewModel(IChangePasswordUseCase changePassword, INavigationService navigation)
    {
        _changePassword = changePassword;
        _navigation = navigation;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (IsBusy) return;
        ErrorMessage = string.Empty;

        if (NewPassword != ConfirmPassword)
        {
            ErrorMessage = "New passwords do not match.";
            return;
        }

        try
        {
            IsBusy = true;
            var result = await _changePassword.ExecuteAsync(new ChangePasswordCommand(CurrentPassword, NewPassword));
            if (!result.IsSuccess)
            {
                ErrorMessage = result.Error ?? "Password change failed.";
                return;
            }

            CurrentPassword = string.Empty;
            NewPassword = string.Empty;
            ConfirmPassword = string.Empty;
            await _navigation.GoToDashboardAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"System error: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }
}
