using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GreenRetail.Core.Terminal;
using GreenRetail.Core.ValueObjects;
using GreenRetail.Data.Entities;
using GreenRetail.Shared.State;
using GreenRetail.Rbac;

namespace GreenRetail.Features.CashSessions;

public partial class CashSessionViewModel : ObservableObject
{
    private readonly IGetActiveSessionQuery _getActiveSession;
    private readonly IOpenRegisterUseCase _openRegister;
    private readonly ICloseRegisterUseCase _closeRegister;
    private readonly IApproveCashVarianceUseCase _approveVariance;
    private readonly IAuthorizationService _authorization;
    private readonly ICurrentUserService _currentUser;
    private readonly ITerminalContext _terminalContext;

    [ObservableProperty] private string statusMessage = string.Empty;
    [ObservableProperty] private string openingCashText = "0";
    [ObservableProperty] private string countedCashText = "0";
    [ObservableProperty] private bool isSessionOpen = false;
    [ObservableProperty] private bool isCounting = false;
    public bool IsNotCounting => !IsCounting;
    partial void OnIsCountingChanged(bool value) => OnPropertyChanged(nameof(IsNotCounting));
    [ObservableProperty] private bool canApproveVariance = false;
    [ObservableProperty] private string sessionActionText = "";
    [ObservableProperty] private CashSession? currentSession;

    public ObservableCollection<CashSession> RecentSessions { get; } = new();

    public CashSessionViewModel(
        IGetActiveSessionQuery getActiveSession,
        IOpenRegisterUseCase openRegister,
        ICloseRegisterUseCase closeRegister,
        IApproveCashVarianceUseCase approveVariance,
        ICurrentUserService currentUser,
        ITerminalContext terminalContext,
        IAuthorizationService authorization)
    {
        _getActiveSession = getActiveSession;
        _openRegister = openRegister;
        _closeRegister = closeRegister;
        _approveVariance = approveVariance;
        _currentUser = currentUser;
        _terminalContext = terminalContext;
        _authorization = authorization;
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
            IsCounting = result.Value.Status == CashSessionStatus.Counting;
            canApproveVariance = false;
            if (_currentUser.UserId.HasValue && _terminalContext.BranchId.HasValue)
                CanApproveVariance = await _authorization.HasPermissionAsync(_currentUser.UserId.Value, PermissionCodes.CashVarianceApprove, _terminalContext.BranchId.Value);
            var expected = new Money(result.Value.ExpectedCashKobo);
            SessionActionText = IsCounting ? "Accounts blind count required" : "Manager can send this session for blind count";
            StatusMessage = IsCounting ? $"Session awaiting blind count. Expected cash is hidden from the counter." : $"Session open for {result.Value.CashierName}.";
        }
        else
        {
            CurrentSession = null;
            IsSessionOpen = false;
            IsCounting = false;
            CanApproveVariance = false;
            SessionActionText = "";
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
        if (!IsSessionOpen || CurrentSession is null) { StatusMessage = "No active session."; return; }

        if (IsCounting)
        {
            if (!decimal.TryParse(CountedCashText, out var cash) || cash < 0)
            { StatusMessage = "Enter the blind counted cash amount."; return; }

            var result = await _closeRegister.ExecuteAsync(new CloseRegisterCommand(
                _currentUser.UserId ?? Guid.Empty, Money.FromNaira(cash).Kobo));
            if (!result.IsSuccess) { StatusMessage = result.Error!; return; }

            var variance = result.Value.VarianceKobo ?? 0;
            StatusMessage = result.Value.Status == CashSessionStatus.Closed
                ? $"Session closed. Variance: {new Money(variance)}"
                : $"Count recorded. Variance: {new Money(variance)}. Approval is required before closing.";
            if (result.Value.Status == CashSessionStatus.Closed) CountedCashText = "0";
            await RefreshSessionStatusAsync();
            return;
        }

        var request = await _closeRegister.ExecuteAsync(new CloseRegisterCommand(_currentUser.UserId ?? Guid.Empty, 0));
        StatusMessage = request.IsSuccess ? "Session sent for Accounts blind count." : request.Error!;
        if (request.IsSuccess) await RefreshSessionStatusAsync();
    }

    [RelayCommand]
    private async Task ApproveVarianceAsync()
    {
        if (CurrentSession is null || CurrentSession.Status != CashSessionStatus.Counting)
        { StatusMessage = "No session is awaiting variance approval."; return; }

        var result = await _approveVariance.ExecuteAsync(new ApproveCashVarianceCommand(CurrentSession.Id));
        StatusMessage = result.IsSuccess ? "Variance approved. Session closed." : result.Error!;
        if (result.IsSuccess) await RefreshSessionStatusAsync();
    }
}