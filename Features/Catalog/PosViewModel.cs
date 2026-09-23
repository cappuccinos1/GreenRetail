using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GreenRetail.Data;
using GreenRetail.Core.Terminal;
using GreenRetail.Features.Cart;
using GreenRetail.Features.CashSessions;
using GreenRetail.Shared.Navigation;
using GreenRetail.Shared.State;

namespace GreenRetail.Features.Catalog;

public partial class PosViewModel : ObservableObject
{
    private readonly IStartupService _startup;
    private readonly ISearchProductsQuery _searchProducts;
    private readonly ICurrentUserService _currentUser;
    private readonly INavigationService _navigation;
    private readonly ICartService _cart;
    private readonly IGetActiveSessionQuery _getActiveSession;
    private readonly ITerminalContext _terminalContext;

    [ObservableProperty] private string? searchText;
    [ObservableProperty] private string statusMessage = string.Empty;
    [ObservableProperty] private string currentUserName = string.Empty;
    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private bool isSessionOpen;
    [ObservableProperty] private string sessionStatusText = "Checking register status...";
    [ObservableProperty] private CartLine? selectedLine;
    [ObservableProperty] private string quantityInput = "—";

    public ObservableCollection<ProductSummary> Products { get; } = new();
    public ICartService Cart => _cart;

    public PosViewModel(
        IStartupService startup,
        ISearchProductsQuery searchProducts,
        ICurrentUserService currentUser,
        INavigationService navigation,
        ICartService cart,
        IGetActiveSessionQuery getActiveSession,
        ITerminalContext terminalContext)
    {
        _startup = startup;
        _searchProducts = searchProducts;
        _currentUser = currentUser;
        _navigation = navigation;
        _cart = cart;
        _getActiveSession = getActiveSession;
        _terminalContext = terminalContext;
    }

    partial void OnSelectedLineChanged(CartLine? value)
    {
        QuantityInput = value is null ? "—" : value.Quantity.ToString("0.##");
    }

    public async Task InitializeAsync()
    {
        try
        {
            IsBusy = true;
            await _startup.InitializeAsync();

            if (!_currentUser.IsAuthenticated)
            {
                await _navigation.GoToLoginAsync();
                return;
            }

            CurrentUserName = _currentUser.DisplayName ?? "Unknown";
            await CheckSessionAsync();
            await SearchAsync();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Startup error: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task CheckSessionAsync()
    {
        var result = await _getActiveSession.ExecuteAsync(_terminalContext.TerminalId);
        IsSessionOpen = result.IsSuccess && result.Value?.Status == CashSessionStatus.Open;
        SessionStatusText = IsSessionOpen
            ? $"Register open · {result.Value!.CashierName}"
            : result.IsSuccess && result.Value?.Status == CashSessionStatus.Counting
                ? "Register is in blind-counting workflow."
                : "Register closed · open a cash session before selling.";
    }

    [RelayCommand]
    private async Task SearchAsync()
    {
        try
        {
            IsBusy = true;
            var result = await _searchProducts.ExecuteAsync(new SearchProductsQueryRequest(SearchText));

            if (!result.IsSuccess)
            {
                StatusMessage = result.Error ?? "Search failed.";
                return;
            }

            Products.Clear();
            foreach (var product in result.Value)
                Products.Add(product);

            StatusMessage = Products.Count == 0
                ? "No products found."
                : $"{Products.Count} product(s) loaded.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Search error: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void AddToCart(ProductSummary product)
    {
        if (!IsSessionOpen)
        {
            StatusMessage = "Register is not open. Open a cash session before selling.";
            return;
        }

        if (product is null)
            return;

        _cart.Add(product.Id, product.Name, product.SellingPrice, product.IsWeighed);
        SelectedLine = _cart.Lines.FirstOrDefault(x => x.ProductId == product.Id);
        StatusMessage = $"{product.Name} added to current sale.";
    }

    [RelayCommand]
    private void Keypad(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return;

        if (QuantityInput == "—")
            QuantityInput = string.Empty;

        if (key == "." && QuantityInput.Contains('.'))
            return;

        if (QuantityInput == "0" && key != ".")
            QuantityInput = key;
        else
            QuantityInput += key;
    }

    [RelayCommand]
    private void Backspace()
    {
        if (QuantityInput == "—" || QuantityInput.Length == 0)
            return;

        QuantityInput = QuantityInput[..^1];
        if (QuantityInput.Length == 0)
            QuantityInput = "—";
    }

    [RelayCommand]
    private void ApplyQuantity()
    {
        if (SelectedLine is null || !decimal.TryParse(QuantityInput, out var quantity))
            return;

        if (quantity <= 0m)
        {
            _cart.Remove(SelectedLine.ProductId);
            SelectedLine = null;
            QuantityInput = "—";
            return;
        }

        _cart.SetQuantity(SelectedLine.ProductId, quantity);
        SelectedLine = _cart.Lines.FirstOrDefault(x => x.ProductId == SelectedLine.ProductId);
        StatusMessage = "Quantity updated.";
    }

    [RelayCommand]
    private void Increment()
    {
        if (SelectedLine is null)
            return;

        _cart.SetQuantity(SelectedLine.ProductId, SelectedLine.Quantity + 1m);
        SelectedLine = _cart.Lines.FirstOrDefault(x => x.ProductId == SelectedLine.ProductId);
    }

    [RelayCommand]
    private void Decrement()
    {
        if (SelectedLine is null)
            return;

        var quantity = SelectedLine.Quantity - 1m;
        if (quantity <= 0m)
        {
            _cart.Remove(SelectedLine.ProductId);
            SelectedLine = null;
            QuantityInput = "—";
            return;
        }

        _cart.SetQuantity(SelectedLine.ProductId, quantity);
        SelectedLine = _cart.Lines.FirstOrDefault(x => x.ProductId == SelectedLine.ProductId);
    }

    [RelayCommand]
    private void Multiply()
    {
        if (SelectedLine is null || !decimal.TryParse(QuantityInput, out var factor) || factor <= 0m)
            return;

        _cart.SetQuantity(SelectedLine.ProductId, SelectedLine.Quantity * factor);
        SelectedLine = _cart.Lines.FirstOrDefault(x => x.ProductId == SelectedLine.ProductId);
        QuantityInput = SelectedLine?.Quantity.ToString("0.##") ?? "—";
    }

    [RelayCommand]
    private void Hold() => StatusMessage = "Hold workflow is reserved for the sales-suspension slice.";

    [RelayCommand]
    private void Print() => StatusMessage = "Receipt printing is available after payment.";

    [RelayCommand]
    private async Task CheckoutAsync()
    {
        if (!IsSessionOpen)
        {
            StatusMessage = "Register is not open. Open a cash session before checkout.";
            return;
        }

        if (_cart.Lines.Count == 0)
        {
            StatusMessage = "Current sale is empty.";
            return;
        }

        await _navigation.GoToCheckoutAsync();
    }

    [RelayCommand]
    private async Task BackAsync() => await _navigation.GoBackAsync();

    [RelayCommand]
    private async Task LogoutAsync()
    {
        _currentUser.Clear();
        await _navigation.GoToLoginAsync();
    }
}
