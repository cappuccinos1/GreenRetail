using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GreenRetail.Core.Abstractions;
using GreenRetail.Core.ValueObjects;
using GreenRetail.Shared.State;

namespace GreenRetail.Features.CashSessions;

public partial class CashSessionViewModel : ObservableObject
{
    private readonly IGetOpenCashSessionQuery _getOpenSession;
    private readonly IOpenCashSessionUseCase _openSession;
    private readonly ICloseCashSessionUseCase _closeSession;
    private readonly ICurrentUserService _currentUser;

    [ObservableProperty]
    private string openingCashInput = "0";

    [ObservableProperty]
    private string countedCashInput = "0";

    [ObservableProperty]
    private string sessionStatus = "No open cash session";

    [ObservableProperty]
    private string statusMessage = string.Empty;

    [ObservableProperty]
    private Money expectedCash;

    [ObservableProperty]
    private Money variance;

    [ObservableProperty]
    private bool isBusy;

    public CashSessionViewModel(
        IGetOpenCashSessionQuery getOpenSession,
        IOpenCashSessionUseCase openSession,
        ICloseCashSessionUseCase closeSession,
        ICurrentUserService currentUser)
    {
        _getOpenSession = getOpenSession;
        _openSession = openSession;
        _closeSession = closeSession;
        _currentUser = currentUser;
    }

    public async Task InitializeAsync()
    {
        await RefreshAsync();
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        try
        {
            IsBusy = true;

            var result = await _getOpenSession.ExecuteAsync(new EmptyRequest());

            if (!result.IsSuccess)
            {
                StatusMessage = result.Error ?? "Unable to load cash session.";
                return;
            }

            var session = result.Value;

            if (session is null)
            {
                SessionStatus = "No open cash session";
                ExpectedCash = Money.Zero;
                Variance = Money.Zero;
                return;
            }

            SessionStatus = $"Open session: {session.CashierName}";
            ExpectedCash = session.ExpectedCash;
            Variance = session.Variance ?? Money.Zero;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task OpenSessionAsync()
    {
        try
        {
            IsBusy = true;
            StatusMessage = string.Empty;

            var opening = ParseMoney(OpeningCashInput);

            var result = await _openSession.ExecuteAsync(new OpenCashSessionCommand(
                opening,
                _currentUser.UserId,
                _currentUser.DisplayName ?? "Unknown"));

            if (!result.IsSuccess)
            {
                StatusMessage = result.Error ?? "Unable to open cash session.";
                return;
            }

            StatusMessage = "Cash session opened.";
            await RefreshAsync();
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task CloseSessionAsync()
    {
        try
        {
            IsBusy = true;
            StatusMessage = string.Empty;

            var counted = ParseMoney(CountedCashInput);

            var result = await _closeSession.ExecuteAsync(new CloseCashSessionCommand(
                counted,
                _currentUser.UserId,
                _currentUser.DisplayName ?? "Unknown"));

            if (!result.IsSuccess)
            {
                StatusMessage = result.Error ?? "Unable to close cash session.";
                return;
            }

            StatusMessage = $"Cash session closed. Variance {result.Value.Variance}.";
            await RefreshAsync();
        }
        finally
        {
            IsBusy = false;
        }
    }

    private static Money ParseMoney(string value)
    {
        return decimal.TryParse(value, out var result)
            ? Money.FromNaira(result)
            : Money.Zero;
    }
}