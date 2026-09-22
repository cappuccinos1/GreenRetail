using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GreenRetail.Core.Terminal;
using GreenRetail.Core.Pricing;
using GreenRetail.Core.ValueObjects;
using GreenRetail.Data.Entities;
using GreenRetail.Features.Cart;
using GreenRetail.Features.Catalog;
using GreenRetail.Features.CashSessions;
using GreenRetail.Features.Checkout;
using GreenRetail.Shared.Navigation;
using GreenRetail.Shared.State;

namespace GreenRetail.Features.Register;

public partial class RegisterViewModel : ObservableObject
{
    private readonly ISearchProductsQuery _searchProducts;
    private readonly ICartService _cart;
    private readonly INavigationService _navigation;
    private readonly ICreateSaleUseCase _createSaleUseCase;
    private readonly IGetActiveSessionQuery _getActiveSession;
    private readonly IOpenRegisterUseCase _openRegister;
    private readonly ICurrentUserService _currentUser;
    private readonly ITerminalContext _terminalContext;
    private readonly IPricingPolicy _pricingPolicy;

    [ObservableProperty] private string barcodeInput = string.Empty;
    [ObservableProperty] private string statusMessage = string.Empty;
    [ObservableProperty] private bool isPaymentModalVisible = false;
    [ObservableProperty] private bool isProcessing = false;
    
    // Session State
    [ObservableProperty] private bool isSessionOpen = false;
    [ObservableProperty] private string sessionStatusText = "Checking register status...";
    [ObservableProperty] private string openingCashText = "0";

    public ObservableCollection<ProductSummary> SearchResults { get; } = new();
    public ICartService Cart => _cart;

    public RegisterViewModel(
        ISearchProductsQuery searchProducts,
        ICartService cart,
        INavigationService navigation,
        ICreateSaleUseCase createSaleUseCase,
        IGetActiveSessionQuery getActiveSession,
        IOpenRegisterUseCase openRegister,
        ICurrentUserService currentUser,
        ITerminalContext terminalContext,
        IPricingPolicy pricingPolicy)
    {
        _searchProducts = searchProducts;
        _cart = cart;
        _navigation = navigation;
        _createSaleUseCase = createSaleUseCase;
        _getActiveSession = getActiveSession;
        _openRegister = openRegister;
        _currentUser = currentUser;
        _terminalContext = terminalContext;
        _pricingPolicy = pricingPolicy;
    }

    public async Task InitializeAsync()
    {
        await CheckSessionStatusAsync();
    }

    private async Task CheckSessionStatusAsync()
    {
        var result = await _getActiveSession.ExecuteAsync(_terminalContext.TerminalId);
        if (result.IsSuccess && result.Value != null)
        {
            IsSessionOpen = true;
            SessionStatusText = $"Session Open | Cashier: {result.Value.CashierName}";
        }
        else
        {
            IsSessionOpen = false;
            SessionStatusText = "Register Closed. Enter opening cash if you are authorized to open the session.";
        }
    }

    [RelayCommand]
    private async Task OpenSessionAsync()
    {
        if (!decimal.TryParse(OpeningCashText, out var openingCash) || openingCash < 0)
        {
            StatusMessage = "Enter a valid opening cash amount.";
            return;
        }

        var cmd = new OpenRegisterCommand(
            _currentUser.UserId ?? Guid.Empty, 
            _currentUser.DisplayName ?? "Unknown", 
            Money.FromNaira(openingCash).Kobo);

        var result = await _openRegister.ExecuteAsync(cmd);
        StatusMessage = result.IsSuccess ? "Session opened successfully." : result.Error!;
        
        if (result.IsSuccess) await CheckSessionStatusAsync();
    }

    partial void OnBarcodeInputChanged(string value)
    {
        if (!string.IsNullOrWhiteSpace(value) && value.Length >= 8) _ = ScanOrSearchAsync();
    }

    [RelayCommand]
    private async Task ScanOrSearchAsync()
    {
        if (!IsSessionOpen) { StatusMessage = "Cannot scan. Register is closed."; return; }
        if (string.IsNullOrWhiteSpace(BarcodeInput)) return;

        var result = await _searchProducts.ExecuteAsync(new SearchProductsQueryRequest(BarcodeInput));
        if (result.IsSuccess && result.Value.Count > 0)
        {
            AddToCart(result.Value[0]);
            BarcodeInput = string.Empty;
        }
        else StatusMessage = $"Item '{BarcodeInput}' not found.";
    }

    [RelayCommand]
    private void AddToCart(ProductSummary product)
    {
        if (product is null || !IsSessionOpen) return;
        _cart.Add(product.Id, product.Name, product.SellingPrice, product.IsWeighed);
        StatusMessage = $"Added: {product.Name}";
    }

    [RelayCommand]
    private void RemoveFromCart(CartLine line)
    {
        if (line is null) return;
        _cart.Remove(line.ProductId);
    }

    [RelayCommand]
    private void OpenPaymentModal()
    {
        if (!IsSessionOpen) { StatusMessage = "Cannot process payment. Register is closed."; return; }
        if (_cart.Lines.Count == 0) { StatusMessage = "Cart is empty."; return; }
        IsPaymentModalVisible = true;
    }

    [RelayCommand]
    private void ClosePaymentModal() => IsPaymentModalVisible = false;

    [RelayCommand]
    private async Task CompleteSaleAsync()
    {
        if (!IsSessionOpen || IsProcessing || _cart.Lines.Count == 0) return;

        IsProcessing = true;
        StatusMessage = "Processing sale...";

        try
        {
            var idempotencyKey = Guid.NewGuid().ToString();
            var items = _cart.Lines.Select(l => new CreateSaleItemCommand(l.ProductId, l.Quantity, Money.FromNaira(l.Price).Kobo)).ToList();
            var payableTotal = _pricingPolicy.RoundTotal(_cart.Subtotal);
            var payments = new List<CreateSalePaymentCommand> { new(PaymentMethod.Cash, payableTotal.Kobo, null) };
            var command = new CreateSaleCommand(idempotencyKey, _currentUser.UserId, _currentUser.DisplayName ?? "System", items, payments, payableTotal.Kobo);

            var result = await _createSaleUseCase.ExecuteAsync(command);
            if (result.IsSuccess)
            {
                StatusMessage = $"Sale completed. ID: {result.Value.SaleId}";
                _cart.Clear();
                IsPaymentModalVisible = false;
            }
            else StatusMessage = $"Sale failed: {result.Error}";
        }
        catch (Exception ex) { StatusMessage = $"Error: {ex.Message}"; }
        finally { IsProcessing = false; }
    }

    [RelayCommand]
    private async Task BackToHubAsync() => await _navigation.GoBackAsync();
}