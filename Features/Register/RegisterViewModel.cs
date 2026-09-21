using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GreenRetail.Core.ValueObjects;
using GreenRetail.Data.Entities;
using GreenRetail.Features.Cart;
using GreenRetail.Features.Catalog;
using GreenRetail.Features.Checkout;
using GreenRetail.Shared.Navigation;

namespace GreenRetail.Features.Register;

public partial class RegisterViewModel : ObservableObject
{
    private readonly ISearchProductsQuery _searchProducts;
    private readonly ICartService _cart;
    private readonly INavigationService _navigation;
    private readonly ICreateSaleUseCase _createSaleUseCase;

    [ObservableProperty] private string barcodeInput = string.Empty;
    [ObservableProperty] private string statusMessage = string.Empty;
    [ObservableProperty] private bool isPaymentModalVisible = false;
    [ObservableProperty] private bool isProcessing = false;

    public ObservableCollection<ProductSummary> SearchResults { get; } = new();
    public ICartService Cart => _cart;

    public RegisterViewModel(
        ISearchProductsQuery searchProducts,
        ICartService cart,
        INavigationService navigation,
        ICreateSaleUseCase createSaleUseCase)
    {
        _searchProducts = searchProducts;
        _cart = cart;
        _navigation = navigation;
        _createSaleUseCase = createSaleUseCase;
    }

    partial void OnBarcodeInputChanged(string value)
    {
        if (!string.IsNullOrWhiteSpace(value) && value.Length >= 8)
        {
            _ = ScanOrSearchAsync();
        }
    }

    [RelayCommand]
    private async Task ScanOrSearchAsync()
    {
        if (string.IsNullOrWhiteSpace(BarcodeInput)) return;

        var result = await _searchProducts.ExecuteAsync(new SearchProductsQueryRequest(BarcodeInput));
        
        if (result.IsSuccess && result.Value.Count > 0)
        {
            var product = result.Value[0];
            AddToCart(product);
            BarcodeInput = string.Empty;
        }
        else
        {
            StatusMessage = $"Item '{BarcodeInput}' not found.";
        }
    }

    [RelayCommand]
    private void AddToCart(ProductSummary product)
    {
        if (product is null) return;
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
        if (_cart.Lines.Count == 0)
        {
            StatusMessage = "Cart is empty.";
            return;
        }
        IsPaymentModalVisible = true;
    }

    [RelayCommand]
    private void ClosePaymentModal()
    {
        IsPaymentModalVisible = false;
    }

    [RelayCommand]
    private async Task CompleteSaleAsync()
    {
        if (IsProcessing || _cart.Lines.Count == 0) return;

        IsProcessing = true;
        StatusMessage = "Processing sale...";

        try
        {
            var idempotencyKey = Guid.NewGuid().ToString();

            var items = _cart.Lines.Select(l => new CreateSaleItemCommand(
                l.ProductId,
                l.Quantity,
                l.UnitPrice.Kobo
            )).ToList();

            var payments = new List<CreateSalePaymentCommand>
            {
                new CreateSalePaymentCommand(
                    PaymentMethod.Cash, 
                    _cart.Subtotal.Kobo, 
                    null)
            };

            var command = new CreateSaleCommand(
                idempotencyKey,
                null, 
                "System Cashier",
                items,
                payments
            );

            var result = await _createSaleUseCase.ExecuteAsync(command);

            if (result.IsSuccess)
            {
                StatusMessage = $"Sale Completed! ID: {result.Value.SaleId}";
                _cart.Clear();
                IsPaymentModalVisible = false;
            }
            else
            {
                StatusMessage = $"Sale Failed: {result.Error}";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsProcessing = false;
        }
    }

    [RelayCommand]
    private async Task BackToHubAsync()
    {
        await _navigation.GoBackAsync();
    }
}