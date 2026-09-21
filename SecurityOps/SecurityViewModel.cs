using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GreenRetail.Shared.State;

namespace GreenRetail.SecurityOps;

public partial class SecurityViewModel : ObservableObject
{
    private readonly IGetRecentSalesQuery _getRecentSales;
    private readonly IVerifyReceiptUseCase _verifyReceipt;
    private readonly IReportSecurityIncidentUseCase _reportIncident;
    private readonly ICurrentUserService _currentUser;

    [ObservableProperty]
    private string statusMessage = string.Empty;

    [ObservableProperty]
    private string verificationResultText = string.Empty;

    [ObservableProperty]
    private RecentSaleListItem? selectedSale;

    [ObservableProperty]
    private string selectedSaleText = "No receipt selected.";

    [ObservableProperty]
    private string incidentType = string.Empty;

    [ObservableProperty]
    private string incidentDetails = string.Empty;

    public ObservableCollection<RecentSaleListItem> RecentSales { get; } = new();

    public SecurityViewModel(
        IGetRecentSalesQuery getRecentSales,
        IVerifyReceiptUseCase verifyReceipt,
        IReportSecurityIncidentUseCase reportIncident,
        ICurrentUserService currentUser)
    {
        _getRecentSales = getRecentSales;
        _verifyReceipt = verifyReceipt;
        _reportIncident = reportIncident;
        _currentUser = currentUser;
    }

    partial void OnSelectedSaleChanged(RecentSaleListItem? value)
    {
        SelectedSaleText = value is null
            ? "No receipt selected."
            : $"Selected receipt: {value.Total} by {value.CashierName}";
    }

    public async Task InitializeAsync()
    {
        await RefreshRecentSalesAsync();
    }

    [RelayCommand]
    private async Task RefreshRecentSalesAsync()
    {
        var result = await _getRecentSales.ExecuteAsync(new GetRecentSalesQueryRequest());

        if (!result.IsSuccess)
        {
            StatusMessage = result.Error ?? "Could not load recent sales.";
            return;
        }

        RecentSales.Clear();

        foreach (var item in result.Value)
        {
            RecentSales.Add(item);
        }

        StatusMessage = $"{RecentSales.Count} recent sale(s) loaded.";
    }

    [RelayCommand]
    private async Task VerifySelectedReceiptAsync()
    {
        if (SelectedSale is null)
        {
            StatusMessage = "Select a receipt to verify.";
            return;
        }

        var result = await _verifyReceipt.ExecuteAsync(new VerifyReceiptCommand(
            SelectedSale.Id,
            _currentUser.UserId,
            "Verified from security UI"));

        if (!result.IsSuccess)
        {
            StatusMessage = result.Error ?? "Receipt verification failed.";
            VerificationResultText = string.Empty;
            return;
        }

        var receipt = result.Value;

        VerificationResultText =
            $"Valid receipt. Lines: {receipt.LineCount}. Units: {receipt.TotalQuantity:0.###}. Total: ₦{(receipt.TotalKobo / 100m):N0}.";

        StatusMessage = "Receipt verified.";
    }

    [RelayCommand]
    private async Task ReportIncidentAsync()
    {
        if (string.IsNullOrWhiteSpace(IncidentType))
        {
            StatusMessage = "Incident type is required.";
            return;
        }

        if (string.IsNullOrWhiteSpace(IncidentDetails))
        {
            StatusMessage = "Incident details are required.";
            return;
        }

        var result = await _reportIncident.ExecuteAsync(new ReportSecurityIncidentCommand(
            SelectedSale?.Id,
            _currentUser.UserId,
            IncidentType,
            IncidentDetails));

        StatusMessage = result.IsSuccess
            ? "Security incident logged."
            : result.Error ?? "Security incident logging failed.";

        if (result.IsSuccess)
        {
            IncidentType = string.Empty;
            IncidentDetails = string.Empty;
        }
    }
}