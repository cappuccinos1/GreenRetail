using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace GreenRetail.Accounting;

public sealed record TrialBalanceLineVm(
    string Code,
    string Name,
    string Type,
    string Debit,
    string Credit,
    string Balance);

public partial class TrialBalanceViewModel : ObservableObject
{
    private readonly IGetTrialBalanceQuery _getTrialBalance;

    [ObservableProperty]
    private string statusMessage = string.Empty;

    [ObservableProperty]
    private string summaryText = string.Empty;

    public ObservableCollection<TrialBalanceLineVm> Lines { get; } = new();

    public TrialBalanceViewModel(IGetTrialBalanceQuery getTrialBalance)
    {
        _getTrialBalance = getTrialBalance;
    }

    public async Task InitializeAsync()
    {
        await RefreshAsync();
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        var result = await _getTrialBalance.ExecuteAsync(new TrialBalanceQueryRequest(null));

        if (!result.IsSuccess)
        {
            StatusMessage = result.Error ?? "Could not load trial balance.";
            return;
        }

        var trialBalance = result.Value;

        Lines.Clear();

        foreach (var line in trialBalance.Lines)
        {
            Lines.Add(new TrialBalanceLineVm(
                line.AccountCode,
                line.AccountName,
                line.AccountType.ToString(),
                FormatKobo(line.DebitKobo),
                FormatKobo(line.CreditKobo),
                FormatKobo(line.BalanceKobo)));
        }

        SummaryText =
            $"As of {trialBalance.AsOfUtc:yyyy-MM-dd HH:mm} | " +
            $"Total Debit: {FormatKobo(trialBalance.TotalDebitKobo)} | " +
            $"Total Credit: {FormatKobo(trialBalance.TotalCreditKobo)}";

        StatusMessage = $"{Lines.Count} account(s) loaded.";
    }

    private static string FormatKobo(long kobo)
    {
        return $"₦{(kobo / 100m):N0}";
    }
}