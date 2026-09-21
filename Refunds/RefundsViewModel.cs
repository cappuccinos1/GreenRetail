using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GreenRetail.Shared.State;

namespace GreenRetail.Refunds;

public partial class RefundsViewModel : ObservableObject
{
    private readonly IInitiateRefundRequestUseCase _initiateRefund;
    private readonly IApproveRefundRequestUseCase _approveRefund;
    private readonly IGetPendingRefundsQuery _getPendingRefunds;
    private readonly IIssueStoreCreditVoucherUseCase _issueVoucher;
    private readonly IRedeemStoreCreditVoucherUseCase _redeemVoucher;
    private readonly IGetActiveVouchersQuery _getActiveVouchers;
    private readonly ICurrentUserService _currentUser;

    [ObservableProperty]
    private string statusMessage = string.Empty;

    [ObservableProperty]
    private string refundSaleIdText = string.Empty;

    [ObservableProperty]
    private string refundReason = string.Empty;

    [ObservableProperty]
    private string refundAmountText = string.Empty;

    [ObservableProperty]
    private string customerName = string.Empty;

    [ObservableProperty]
    private string customerPhone = string.Empty;

    [ObservableProperty]
    private RefundListItem? selectedRefund;

    [ObservableProperty]
    private string selectedRefundText = "No refund selected.";

    [ObservableProperty]
    private string issueAmountText = string.Empty;

    [ObservableProperty]
    private string redeemCodeText = string.Empty;

    [ObservableProperty]
    private string redeemAmountText = string.Empty;

    public ObservableCollection<RefundListItem> PendingRefunds { get; } = new();
    public ObservableCollection<VoucherListItem> ActiveVouchers { get; } = new();

    public RefundsViewModel(
        IInitiateRefundRequestUseCase initiateRefund,
        IApproveRefundRequestUseCase approveRefund,
        IGetPendingRefundsQuery getPendingRefunds,
        IIssueStoreCreditVoucherUseCase issueVoucher,
        IRedeemStoreCreditVoucherUseCase redeemVoucher,
        IGetActiveVouchersQuery getActiveVouchers,
        ICurrentUserService currentUser)
    {
        _initiateRefund = initiateRefund;
        _approveRefund = approveRefund;
        _getPendingRefunds = getPendingRefunds;
        _issueVoucher = issueVoucher;
        _redeemVoucher = redeemVoucher;
        _getActiveVouchers = getActiveVouchers;
        _currentUser = currentUser;
    }

    partial void OnSelectedRefundChanged(RefundListItem? value)
    {
        SelectedRefundText = value is null
            ? "No refund selected."
            : $"Selected refund: {value.Total} - {value.Reason}";
    }

    public async Task InitializeAsync()
    {
        await RefreshPendingRefundsAsync();
        await RefreshActiveVouchersAsync();
    }

    [RelayCommand]
    private async Task RefreshPendingRefundsAsync()
    {
        var result = await _getPendingRefunds.ExecuteAsync(new GetPendingRefundsQueryRequest());

        if (!result.IsSuccess)
        {
            StatusMessage = result.Error ?? "Could not load refunds.";
            return;
        }

        PendingRefunds.Clear();

        foreach (var item in result.Value)
        {
            PendingRefunds.Add(item);
        }
    }

    [RelayCommand]
    private async Task RefreshActiveVouchersAsync()
    {
        var result = await _getActiveVouchers.ExecuteAsync(new GetActiveVouchersQueryRequest());

        if (!result.IsSuccess)
        {
            StatusMessage = result.Error ?? "Could not load vouchers.";
            return;
        }

        ActiveVouchers.Clear();

        foreach (var item in result.Value)
        {
            ActiveVouchers.Add(item);
        }
    }

    [RelayCommand]
    private async Task InitiateRefundAsync()
    {
        Guid? saleId = null;

        if (!string.IsNullOrWhiteSpace(RefundSaleIdText))
        {
            if (!Guid.TryParse(RefundSaleIdText.Trim(), out var parsedSaleId))
            {
                StatusMessage = "Sale ID is not valid.";
                return;
            }

            saleId = parsedSaleId;
        }

        if (!TryParseKobo(RefundAmountText, out var amountKobo))
        {
            StatusMessage = "Enter a refund amount greater than zero.";
            return;
        }

        if (string.IsNullOrWhiteSpace(RefundReason))
        {
            StatusMessage = "Refund reason is required.";
            return;
        }

        var result = await _initiateRefund.ExecuteAsync(new InitiateRefundRequestCommand(
            saleId,
            _currentUser.UserId,
            RefundReason,
            amountKobo));

        StatusMessage = result.IsSuccess
            ? "Refund request created."
            : result.Error ?? "Refund request failed.";

        if (result.IsSuccess)
        {
            RefundSaleIdText = string.Empty;
            RefundReason = string.Empty;
            RefundAmountText = string.Empty;

            await RefreshPendingRefundsAsync();
        }
    }

    [RelayCommand]
    private async Task ApproveSelectedRefundAsync()
    {
        if (SelectedRefund is null)
        {
            StatusMessage = "Select a refund request to approve.";
            return;
        }

        var result = await _approveRefund.ExecuteAsync(new ApproveRefundRequestCommand(
            SelectedRefund.Id,
            CustomerName,
            CustomerPhone));

        StatusMessage = result.IsSuccess
            ? $"Refund approved. Store credit code: {result.Value.Code}"
            : result.Error ?? "Refund approval failed.";

        if (result.IsSuccess)
        {
            await RefreshPendingRefundsAsync();
            await RefreshActiveVouchersAsync();
        }
    }

    [RelayCommand]
    private async Task IssueVoucherAsync()
    {
        if (!TryParseKobo(IssueAmountText, out var amountKobo))
        {
            StatusMessage = "Enter a voucher amount greater than zero.";
            return;
        }

        var result = await _issueVoucher.ExecuteAsync(new IssueStoreCreditCommand(
            amountKobo,
            CustomerName,
            CustomerPhone,
            null,
            null));

        StatusMessage = result.IsSuccess
            ? $"Voucher issued: {result.Value.Code}"
            : result.Error ?? "Voucher issue failed.";

        if (result.IsSuccess)
        {
            IssueAmountText = string.Empty;
            await RefreshActiveVouchersAsync();
        }
    }

    [RelayCommand]
    private async Task RedeemVoucherAsync()
    {
        if (string.IsNullOrWhiteSpace(RedeemCodeText))
        {
            StatusMessage = "Voucher code is required.";
            return;
        }

        if (!TryParseKobo(RedeemAmountText, out var amountKobo))
        {
            StatusMessage = "Enter a redemption amount greater than zero.";
            return;
        }

        var result = await _redeemVoucher.ExecuteAsync(new RedeemStoreCreditCommand(
            RedeemCodeText,
            amountKobo,
            null));

        StatusMessage = result.IsSuccess
            ? $"Voucher redeemed. Remaining balance: {FormatKobo(result.Value.BalanceKobo)}"
            : result.Error ?? "Voucher redemption failed.";

        if (result.IsSuccess)
        {
            RedeemAmountText = string.Empty;
            await RefreshActiveVouchersAsync();
        }
    }

    private static bool TryParseKobo(string text, out long kobo)
    {
        if (decimal.TryParse(text, out var value) && value > 0m)
        {
            kobo = (long)Math.Round(value * 100m, MidpointRounding.AwayFromZero);
            return true;
        }

        kobo = 0;
        return false;
    }

    private static string FormatKobo(long kobo)
    {
        return $"₦{(kobo / 100m):N0}";
    }
}