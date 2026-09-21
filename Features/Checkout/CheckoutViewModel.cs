using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GreenRetail.Core.ValueObjects;
using GreenRetail.Core.Pricing;
using GreenRetail.Data.Entities;
using GreenRetail.Features.Cart;
using GreenRetail.Features.Printing;
using GreenRetail.Shared.Navigation;
using GreenRetail.Shared.State;

namespace GreenRetail.Features.Checkout;

public partial class CheckoutViewModel : ObservableObject
{
    private readonly ICartService _cart;
    private readonly ICompleteSaleUseCase _completeSale;
    private readonly IPricingPolicy _pricingPolicy;
    private readonly ICurrentUserService _currentUser;
    private readonly INavigationService _navigation;
    private readonly IReceiptPrinter _receiptPrinter;

    [ObservableProperty]
    private string tenderedAmount = "0";

    [ObservableProperty]
    private PaymentMethod selectedPaymentMethod = PaymentMethod.Cash;

    [ObservableProperty]
    private string statusMessage = string.Empty;

    [ObservableProperty]
    private bool isBusy;

    public IReadOnlyList<PaymentMethod> PaymentMethods { get; } =
        Enum.GetValues<PaymentMethod>().ToList();

    public Money Total => _pricingPolicy.RoundTotal(_cart.Subtotal);

    public Money ChangeDue => _pricingPolicy.CalculateChange(Total, ParseTendered());

    public Money BalanceDue => _pricingPolicy.CalculateBalanceDue(Total, ParseTendered());

    public CheckoutViewModel(
        ICartService cart,
        ICompleteSaleUseCase completeSale,
        IPricingPolicy pricingPolicy,
        ICurrentUserService currentUser,
        INavigationService navigation,
        IReceiptPrinter receiptPrinter)
    {
        _cart = cart;
        _completeSale = completeSale;
        _pricingPolicy = pricingPolicy;
        _currentUser = currentUser;
        _navigation = navigation;
        _receiptPrinter = receiptPrinter;
    }

    public void Initialize()
    {
        TenderedAmount = Total.ToNaira().ToString("0");

        OnPropertyChanged(nameof(Total));
        OnPropertyChanged(nameof(ChangeDue));
        OnPropertyChanged(nameof(BalanceDue));
    }

    partial void OnTenderedAmountChanged(string value)
    {
        OnPropertyChanged(nameof(ChangeDue));
        OnPropertyChanged(nameof(BalanceDue));
    }

    [RelayCommand]
    private async Task CompleteSaleAsync()
    {
        if (IsBusy)
            return;

        if (_cart.Lines.Count == 0)
        {
            StatusMessage = "Cart is empty.";
            return;
        }

        try
        {
            IsBusy = true;
            StatusMessage = string.Empty;

            var tendered = ParseTendered();

            var command = new CompleteSaleCommand(
                CustomerId: null,
                CashierId: _currentUser.UserId,
                CashierName: _currentUser.DisplayName ?? "Unknown",
                Items: _cart.Lines
                    .Select(x => new CompleteSaleItem(x.ProductId, x.Quantity))
                    .ToList(),
                Payments: new List<CompleteSalePayment>
                {
                    new(SelectedPaymentMethod, tendered, null)
                });

            var result = await _completeSale.ExecuteAsync(command);

            if (!result.IsSuccess)
            {
                StatusMessage = result.Error ?? "Sale failed.";
                return;
            }

            await _receiptPrinter.PrintAsync(result.Value);

            _cart.Clear();

            StatusMessage =
                $"Sale saved. Change {result.Value.ChangeDue}. Balance {result.Value.BalanceDue}.";

            await _navigation.GoBackAsync();
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task BackAsync()
    {
        await _navigation.GoBackAsync();
    }

    private Money ParseTendered()
    {
        return decimal.TryParse(TenderedAmount, out var value)
            ? Money.FromNaira(value)
            : Money.Zero;
    }
}