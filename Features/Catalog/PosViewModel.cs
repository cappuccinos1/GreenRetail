using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GreenRetail.Data;
using GreenRetail.Features.Cart;
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

    [ObservableProperty]
    private string? searchText;

    [ObservableProperty]
    private string statusMessage = string.Empty;

    [ObservableProperty]
    private string currentUserName = string.Empty;

    [ObservableProperty]
    private bool isBusy;

    public ObservableCollection<ProductSummary> Products { get; } = new();

    public ICartService Cart => _cart;

    public PosViewModel(
        IStartupService startup,
        ISearchProductsQuery searchProducts,
        ICurrentUserService currentUser,
        INavigationService navigation,
        ICartService cart)
    {
        _startup = startup;
        _searchProducts = searchProducts;
        _currentUser = currentUser;
        _navigation = navigation;
        _cart = cart;
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

    [RelayCommand]
    private async Task SearchAsync()
    {
        try
        {
            IsBusy = true;

            var result = await _searchProducts.ExecuteAsync(
                new SearchProductsQueryRequest(SearchText));

            if (!result.IsSuccess)
            {
                StatusMessage = result.Error ?? "Search failed.";
                return;
            }

            Products.Clear();

            foreach (var product in result.Value)
            {
                Products.Add(product);
            }

            StatusMessage = Products.Count == 0
                ? "No products found. Tap Refresh to try again."
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
        if (product is null)
            return;

        _cart.Add(
            product.Id,
            product.Name,
            product.SellingPrice,
            product.IsWeighed);

        StatusMessage = $"{product.Name} added to cart.";
    }

    [RelayCommand]
    private async Task CheckoutAsync()
    {
        if (_cart.Lines.Count == 0)
        {
            StatusMessage = "Cart is empty.";
            return;
        }

        await _navigation.GoToCheckoutAsync();
    }

    [RelayCommand]
    private async Task LogoutAsync()
    {
        _currentUser.Clear();
        await _navigation.GoToLoginAsync();
    }
}