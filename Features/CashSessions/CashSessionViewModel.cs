using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GreenRetail.Core.Terminal;
using GreenRetail.Core.ValueObjects;
using GreenRetail.Data.Entities;
using GreenRetail.Shared.State;

namespace GreenRetail.Features.CashSessions;

public partial class CashSessionViewModel : ObservableObject
{
    private readonly IGetActiveSessionQuery _getActiveSession;
    private readonly IOpenRegisterUseCase _openRegister;
    private readonly ICloseRegisterUseCase _closeRegister;
    private readonly ICurrentUserService _currentUser;
    private readonly ITerminalContext _terminalContext;

    [ObservableProperty] private string statusMessage = string.Empty;
    [ObservableProperty] private string openingCashText = "0";
    [ObservableProperty] private string countedCashText = "0";
    [ObservableProperty] private bool isSessionOpen = false;
    [ObservableProperty] private CashSession? currentSession;

    public ObservableCollection<CashSession> RecentSessions { get; } = new();

    public CashSessionViewModel(
        IGetActiveSessionQuery getActiveSession,
        IOpenRegisterUseCase openRegister,
        ICloseRegisterUseCase closeRegister,
        ICurrentUserService currentUser,
        ITerminalContext terminalContext)
    {
        _getActiveSession = getActiveSession;
        _openRegister = openRegister;
        _closeRegister = closeRegister;
        _currentUser = currentUser;
        _terminalContext = terminalContext;
    }

    public async Task InitializeAsync()
    {
        await RefreshSessionStatusAsync();
    }

    private async Task RefreshSessionStatusAsync()
    {
        var result = await _getActiveSession.ExecuteAsync(_terminalContext.TerminalId);
        if (result.IsSuccess && result.Value != null)
        {
            CurrentSession = result.Value;
            IsSessionOpen = true;
            var expected = new Money(CurrentSession.ExpectedCashKobo);
            StatusMessage = $"Session open for {CurrentSession.CashierName}. Expected Cash: {expected}";
        }
        else
        {
            CurrentSession = null;
            IsSessionOpen = false;
            StatusMessage = "No open session on this register.";
        }
    }

    [RelayCommand]
    private async Task OpenSessionAsync()
    {
        if (!decimal.TryParse(OpeningCashText, out var cash) || cash < 0)
        {
            StatusMessage = "Enter a valid opening cash amount.";
            return;
        }

        var cmd = new OpenRegisterCommand(
            _currentUser.UserId ?? Guid.Empty,
            _currentUser.DisplayName ?? "Unknown",
            Money.FromNaira(cash).Kobo);

        var result = await _openRegister.ExecuteAsync(cmd);
        StatusMessage = result.IsSuccess ? "Session opened." : result.Error!;
        if (result.IsSuccess) await RefreshSessionStatusAsync();
    }

    [RelayCommand]
    private async Task CloseSessionAsync()
    {
        if (!decimal.TryParse(CountedCashText, out var cash) || cash < 0)
        {
            StatusMessage = "Enter a valid counted cash amount.";
            return;
        }

        var cmd = new CloseRegisterCommand(
            _currentUser.UserId ?? Guid.Empty,
            Money.FromNaira(cash).Kobo);

        var result = await _closeRegister.ExecuteAsync(cmd);
        if (result.IsSuccess)
        {
            var variance = result.Value.VarianceKobo ?? 0;
            var varianceMoney = new Money(variance);
            StatusMessage = $"Session closed. Variance: {varianceMoney}";
        }
        else
        {
            StatusMessage = result.Error!;
        }
        
        if (result.IsSuccess) await RefreshSessionStatusAsync();
    }
}