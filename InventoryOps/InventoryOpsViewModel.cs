using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GreenRetail.Features.Catalog;
using GreenRetail.Shared.State;

namespace GreenRetail.InventoryOps;

public partial class InventoryOpsViewModel : ObservableObject
{
    private readonly ISearchProductsQuery _searchProducts;
    private readonly IRequestStockAdjustmentUseCase _requestStockAdjustment;
    private readonly IApproveStockAdjustmentUseCase _approveStockAdjustment;
    private readonly IGetPendingStockAdjustmentsQuery _getPendingAdjustments;
    private readonly IRequestStockOverrideUseCase _requestStockOverride;
    private readonly IResolveStockOverrideUseCase _resolveStockOverride;
    private readonly IGetPendingStockOverridesQuery _getPendingOverrides;
    private readonly ICurrentUserService _currentUser;

    [ObservableProperty]
    private string statusMessage = string.Empty;

    [ObservableProperty]
    private string? searchText;

    [ObservableProperty]
    private ProductSummary? selectedProduct;

    [ObservableProperty]
    private string selectedProductText = "No product selected.";

    [ObservableProperty]
    private string adjustmentQuantityText = string.Empty;

    [ObservableProperty]
    private string adjustmentReason = string.Empty;

    [ObservableProperty]
    private string adjustmentNote = string.Empty;

    [ObservableProperty]
    private StockAdjustmentListItem? selectedAdjustment;

    [ObservableProperty]
    private string selectedAdjustmentText = "No stock adjustment selected.";

    [ObservableProperty]
    private StockOverrideListItem? selectedOverride;

    [ObservableProperty]
    private string selectedOverrideText = "No stock override selected.";

    [ObservableProperty]
    private string overrideQuantityText = string.Empty;

    public ObservableCollection<ProductSummary> Products { get; } = new();
    public ObservableCollection<StockAdjustmentListItem> PendingAdjustments { get; } = new();
    public ObservableCollection<StockOverrideListItem> PendingOverrides { get; } = new();

    public InventoryOpsViewModel(
        ISearchProductsQuery searchProducts,
        IRequestStockAdjustmentUseCase requestStockAdjustment,
        IApproveStockAdjustmentUseCase approveStockAdjustment,
        IGetPendingStockAdjustmentsQuery getPendingAdjustments,
        IRequestStockOverrideUseCase requestStockOverride,
        IResolveStockOverrideUseCase resolveStockOverride,
        IGetPendingStockOverridesQuery getPendingOverrides,
        ICurrentUserService currentUser)
    {
        _searchProducts = searchProducts;
        _requestStockAdjustment = requestStockAdjustment;
        _approveStockAdjustment = approveStockAdjustment;
        _getPendingAdjustments = getPendingAdjustments;
        _requestStockOverride = requestStockOverride;
        _resolveStockOverride = resolveStockOverride;
        _getPendingOverrides = getPendingOverrides;
        _currentUser = currentUser;
    }

    partial void OnSelectedProductChanged(ProductSummary? value)
    {
        SelectedProductText = value is null
            ? "No product selected."
            : $"Selected: {value.Name} ({value.Sku})";
    }

    partial void OnSelectedAdjustmentChanged(StockAdjustmentListItem? value)
    {
        SelectedAdjustmentText = value is null
            ? "No stock adjustment selected."
            : $"Selected adjustment: {value.ProductName}";
    }

    partial void OnSelectedOverrideChanged(StockOverrideListItem? value)
    {
        SelectedOverrideText = value is null
            ? "No stock override selected."
            : $"Selected override: {value.ProductName}";
    }

    public async Task InitializeAsync()
    {
        await SearchProductsAsync();
        await RefreshAdjustmentsAsync();
        await RefreshOverridesAsync();
    }

    [RelayCommand]
    private async Task SearchProductsAsync()
    {
        var result = await _searchProducts.ExecuteAsync(
            new SearchProductsQueryRequest(SearchText));

        if (!result.IsSuccess)
        {
            StatusMessage = result.Error ?? "Product search failed.";
            return;
        }

        Products.Clear();

        foreach (var product in result.Value)
        {
            Products.Add(product);
        }

        StatusMessage = $"{Products.Count} product(s) loaded.";
    }

    [RelayCommand]
    private async Task RequestStockAdjustmentAsync()
    {
        if (SelectedProduct is null)
        {
            StatusMessage = "Select a product first.";
            return;
        }

        if (!decimal.TryParse(AdjustmentQuantityText, out var quantity) || quantity == 0m)
        {
            StatusMessage = "Enter a non-zero adjustment quantity.";
            return;
        }

        if (string.IsNullOrWhiteSpace(AdjustmentReason))
        {
            StatusMessage = "Adjustment reason is required.";
            return;
        }

        var result = await _requestStockAdjustment.ExecuteAsync(new RequestStockAdjustmentCommand(
            SelectedProduct.Id,
            quantity,
            AdjustmentReason,
            AdjustmentNote,
            _currentUser.UserId,
            null));

        StatusMessage = result.IsSuccess
            ? "Stock adjustment requested."
            : result.Error ?? "Stock adjustment request failed.";

        if (result.IsSuccess)
        {
            AdjustmentQuantityText = string.Empty;
            AdjustmentReason = string.Empty;
            AdjustmentNote = string.Empty;

            await RefreshAdjustmentsAsync();
        }
    }

    [RelayCommand]
    private async Task RefreshAdjustmentsAsync()
    {
        var result = await _getPendingAdjustments.ExecuteAsync(
            new GetPendingStockAdjustmentsQueryRequest());

        if (!result.IsSuccess)
        {
            StatusMessage = result.Error ?? "Could not load stock adjustments.";
            return;
        }

        PendingAdjustments.Clear();

        foreach (var item in result.Value)
        {
            PendingAdjustments.Add(item);
        }
    }

    [RelayCommand]
    private async Task ApproveSelectedAdjustmentAsync()
    {
        if (SelectedAdjustment is null)
        {
            StatusMessage = "Select a stock adjustment to approve.";
            return;
        }

        var result = await _approveStockAdjustment.ExecuteAsync(new ApproveStockAdjustmentCommand(
            SelectedAdjustment.Id,
            _currentUser.UserId));

        StatusMessage = result.IsSuccess
            ? "Stock adjustment approved and posted."
            : result.Error ?? "Approval failed.";

        if (result.IsSuccess)
        {
            await RefreshAdjustmentsAsync();
        }
    }

    [RelayCommand]
    private async Task RequestStockOverrideAsync()
    {
        if (SelectedProduct is null)
        {
            StatusMessage = "Select a product first.";
            return;
        }

        var result = await _requestStockOverride.ExecuteAsync(new RequestStockOverrideCommand(
            SelectedProduct.Id,
            _currentUser.UserId,
            null,
            "POS out-of-stock override request"));

        StatusMessage = result.IsSuccess
            ? "Stock override requested."
            : result.Error ?? "Stock override request failed.";

        if (result.IsSuccess)
        {
            await RefreshOverridesAsync();
        }
    }

    [RelayCommand]
    private async Task RefreshOverridesAsync()
    {
        var result = await _getPendingOverrides.ExecuteAsync(
            new GetPendingStockOverridesQueryRequest());

        if (!result.IsSuccess)
        {
            StatusMessage = result.Error ?? "Could not load stock overrides.";
            return;
        }

        PendingOverrides.Clear();

        foreach (var item in result.Value)
        {
            PendingOverrides.Add(item);
        }
    }

    [RelayCommand]
    private async Task ResolveSelectedOverrideAsync()
    {
        if (SelectedOverride is null)
        {
            StatusMessage = "Select a stock override to resolve.";
            return;
        }

        if (!decimal.TryParse(OverrideQuantityText, out var quantity) || quantity <= 0m)
        {
            StatusMessage = "Enter a quantity greater than zero.";
            return;
        }

        var result = await _resolveStockOverride.ExecuteAsync(new ResolveStockOverrideCommand(
            SelectedOverride.Id,
            quantity,
            _currentUser.UserId));

        StatusMessage = result.IsSuccess
            ? "Stock override resolved."
            : result.Error ?? "Override resolution failed.";

        if (result.IsSuccess)
        {
            OverrideQuantityText = string.Empty;
            await RefreshOverridesAsync();
        }
    }
}