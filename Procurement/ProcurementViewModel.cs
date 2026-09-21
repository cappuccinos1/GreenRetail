using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GreenRetail.Features.Catalog;
using GreenRetail.Shared.State;

namespace GreenRetail.Procurement;

public sealed record NewPurchaseOrderLineItem(
    Guid ProductId,
    string ProductName,
    string Quantity,
    string UnitCost);

public sealed record NewGrnLineItem(
    Guid ProductId,
    string ProductName,
    string Accepted,
    string Rejected,
    string Expiry,
    string Batch);

public partial class ProcurementViewModel : ObservableObject
{
    private readonly ISearchProductsQuery _searchProducts;
    private readonly ICreateSupplierUseCase _createSupplier;
    private readonly IGetSuppliersQuery _getSuppliers;
    private readonly ICreatePurchaseOrderUseCase _createPurchaseOrder;
    private readonly IGetActivePurchaseOrdersQuery _getActivePurchaseOrders;
    private readonly IPostGrnUseCase _postGrn;
    private readonly ICurrentUserService _currentUser;

    [ObservableProperty]
    private string statusMessage = string.Empty;

    [ObservableProperty]
    private string? productSearchText;

    [ObservableProperty]
    private ProductSummary? selectedProduct;

    [ObservableProperty]
    private string selectedProductText = "No product selected.";

    [ObservableProperty]
    private string supplierName = string.Empty;

    [ObservableProperty]
    private string supplierPhone = string.Empty;

    [ObservableProperty]
    private string supplierEmail = string.Empty;

    [ObservableProperty]
    private string supplierAddress = string.Empty;

    [ObservableProperty]
    private SupplierListItem? selectedSupplier;

    [ObservableProperty]
    private string selectedSupplierText = "No supplier selected.";

    [ObservableProperty]
    private string poQuantityText = string.Empty;

    [ObservableProperty]
    private string poUnitCostText = string.Empty;

    [ObservableProperty]
    private string poNotes = string.Empty;

    [ObservableProperty]
    private PurchaseOrderListItem? selectedPurchaseOrder;

    [ObservableProperty]
    private string selectedPurchaseOrderText = "No purchase order selected.";

    [ObservableProperty]
    private string vendorInvoiceNumber = string.Empty;

    [ObservableProperty]
    private string vendorInvoiceDateText = string.Empty;

    [ObservableProperty]
    private string grnNotes = string.Empty;

    [ObservableProperty]
    private string grnAcceptedQuantityText = string.Empty;

    [ObservableProperty]
    private string grnRejectedQuantityText = string.Empty;

    [ObservableProperty]
    private string grnExpiryText = string.Empty;

    [ObservableProperty]
    private string grnBatchText = string.Empty;

    public ObservableCollection<ProductSummary> Products { get; } = new();
    public ObservableCollection<SupplierListItem> Suppliers { get; } = new();
    public ObservableCollection<PurchaseOrderListItem> ActivePurchaseOrders { get; } = new();
    public ObservableCollection<NewPurchaseOrderLineItem> PurchaseOrderLines { get; } = new();
    public ObservableCollection<NewGrnLineItem> GrnLines { get; } = new();

    public ProcurementViewModel(
        ISearchProductsQuery searchProducts,
        ICreateSupplierUseCase createSupplier,
        IGetSuppliersQuery getSuppliers,
        ICreatePurchaseOrderUseCase createPurchaseOrder,
        IGetActivePurchaseOrdersQuery getActivePurchaseOrders,
        IPostGrnUseCase postGrn,
        ICurrentUserService currentUser)
    {
        _searchProducts = searchProducts;
        _createSupplier = createSupplier;
        _getSuppliers = getSuppliers;
        _createPurchaseOrder = createPurchaseOrder;
        _getActivePurchaseOrders = getActivePurchaseOrders;
        _postGrn = postGrn;
        _currentUser = currentUser;
    }

    partial void OnSelectedProductChanged(ProductSummary? value)
    {
        SelectedProductText = value is null
            ? "No product selected."
            : $"Selected product: {value.Name} ({value.Sku})";
    }

    partial void OnSelectedSupplierChanged(SupplierListItem? value)
    {
        SelectedSupplierText = value is null
            ? "No supplier selected."
            : $"Selected supplier: {value.Name}";
    }

    partial void OnSelectedPurchaseOrderChanged(PurchaseOrderListItem? value)
    {
        SelectedPurchaseOrderText = value is null
            ? "No purchase order selected."
            : $"Selected PO: {value.Number} - {value.SupplierName}";
    }

    public async Task InitializeAsync()
    {
        await SearchProductsAsync();
        await RefreshSuppliersAsync();
        await RefreshActivePurchaseOrdersAsync();
    }

    [RelayCommand]
    private async Task SearchProductsAsync()
    {
        var result = await _searchProducts.ExecuteAsync(
            new SearchProductsQueryRequest(ProductSearchText));

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
    private async Task RefreshSuppliersAsync()
    {
        var result = await _getSuppliers.ExecuteAsync(new GetSuppliersQueryRequest(null));

        if (!result.IsSuccess)
        {
            StatusMessage = result.Error ?? "Could not load suppliers.";
            return;
        }

        Suppliers.Clear();

        foreach (var supplier in result.Value)
        {
            Suppliers.Add(supplier);
        }
    }

    [RelayCommand]
    private async Task RefreshActivePurchaseOrdersAsync()
    {
        var result = await _getActivePurchaseOrders.ExecuteAsync(new GetActivePurchaseOrdersQueryRequest());

        if (!result.IsSuccess)
        {
            StatusMessage = result.Error ?? "Could not load purchase orders.";
            return;
        }

        ActivePurchaseOrders.Clear();

        foreach (var purchaseOrder in result.Value)
        {
            ActivePurchaseOrders.Add(purchaseOrder);
        }
    }

    [RelayCommand]
    private async Task CreateSupplierAsync()
    {
        if (string.IsNullOrWhiteSpace(SupplierName))
        {
            StatusMessage = "Supplier name is required.";
            return;
        }

        var result = await _createSupplier.ExecuteAsync(new CreateSupplierCommand(
            SupplierName,
            SupplierPhone,
            SupplierEmail,
            SupplierAddress));

        StatusMessage = result.IsSuccess
            ? $"Supplier '{result.Value.Name}' created."
            : result.Error ?? "Supplier creation failed.";

        if (result.IsSuccess)
        {
            SupplierName = string.Empty;
            SupplierPhone = string.Empty;
            SupplierEmail = string.Empty;
            SupplierAddress = string.Empty;

            await RefreshSuppliersAsync();
        }
    }

    [RelayCommand]
    private void AddPurchaseOrderLine()
    {
        if (SelectedProduct is null)
        {
            StatusMessage = "Select a product before adding a PO line.";
            return;
        }

        if (!decimal.TryParse(PoQuantityText, out var quantity) || quantity <= 0m)
        {
            StatusMessage = "Enter a valid PO quantity.";
            return;
        }

        if (!decimal.TryParse(PoUnitCostText, out var unitCost) || unitCost < 0m)
        {
            StatusMessage = "Enter a valid unit cost.";
            return;
        }

        PurchaseOrderLines.Add(new NewPurchaseOrderLineItem(
            SelectedProduct.Id,
            SelectedProduct.Name,
            quantity.ToString("0.###"),
            unitCost.ToString("0.00")));

        PoQuantityText = string.Empty;
        PoUnitCostText = string.Empty;

        StatusMessage = "PO line added.";
    }

    [RelayCommand]
    private async Task CreatePurchaseOrderAsync()
    {
        if (SelectedSupplier is null)
        {
            StatusMessage = "Select a supplier.";
            return;
        }

        if (PurchaseOrderLines.Count == 0)
        {
            StatusMessage = "Add at least one purchase order line.";
            return;
        }

        var lines = PurchaseOrderLines
            .Select(x => new CreatePurchaseOrderLineInput(
                x.ProductId,
                ParseDecimal(x.Quantity),
                ParseDecimal(x.UnitCost)))
            .ToList();

        var result = await _createPurchaseOrder.ExecuteAsync(new CreatePurchaseOrderCommand(
            SelectedSupplier.Id,
            PoNotes,
            _currentUser.UserId,
            null,
            lines));

        StatusMessage = result.IsSuccess
            ? $"Purchase order {result.Value.Number} created."
            : result.Error ?? "Purchase order creation failed.";

        if (result.IsSuccess)
        {
            PurchaseOrderLines.Clear();
            PoNotes = string.Empty;

            await RefreshActivePurchaseOrdersAsync();
        }
    }

    [RelayCommand]
    private void AddGrnLine()
    {
        if (SelectedProduct is null)
        {
            StatusMessage = "Select a product before adding a GRN line.";
            return;
        }

        if (!decimal.TryParse(GrnAcceptedQuantityText, out var accepted) || accepted < 0m)
        {
            StatusMessage = "Enter a valid accepted quantity.";
            return;
        }

        if (!decimal.TryParse(GrnRejectedQuantityText, out var rejected) || rejected < 0m)
        {
            StatusMessage = "Enter a valid rejected quantity.";
            return;
        }

        if (accepted == 0m && rejected == 0m)
        {
            StatusMessage = "Accepted or rejected quantity must be greater than zero.";
            return;
        }

        GrnLines.Add(new NewGrnLineItem(
            SelectedProduct.Id,
            SelectedProduct.Name,
            accepted.ToString("0.###"),
            rejected.ToString("0.###"),
            GrnExpiryText,
            GrnBatchText));

        GrnAcceptedQuantityText = string.Empty;
        GrnRejectedQuantityText = string.Empty;
        GrnExpiryText = string.Empty;
        GrnBatchText = string.Empty;

        StatusMessage = "GRN line added.";
    }

    [RelayCommand]
    private async Task PostGrnAsync()
    {
        if (GrnLines.Count == 0)
        {
            StatusMessage = "Add at least one GRN line.";
            return;
        }

        if (string.IsNullOrWhiteSpace(VendorInvoiceNumber))
        {
            StatusMessage = "Vendor invoice number is required.";
            return;
        }

        Guid supplierId;

        if (SelectedPurchaseOrder is not null)
        {
            supplierId = SelectedPurchaseOrder.SupplierId;
        }
        else if (SelectedSupplier is not null)
        {
            supplierId = SelectedSupplier.Id;
        }
        else
        {
            StatusMessage = "Select a supplier or purchase order.";
            return;
        }

        if (!DateTime.TryParse(VendorInvoiceDateText, out var invoiceDate))
        {
            invoiceDate = DateTime.UtcNow;
        }

        var lines = GrnLines
            .Select(x => new PostGrnLineInput(
                x.ProductId,
                ParseDecimal(x.Accepted),
                ParseDecimal(x.Rejected),
                null,
                ParseNullableDate(x.Expiry),
                string.IsNullOrWhiteSpace(x.Batch) ? null : x.Batch))
            .ToList();

        var result = await _postGrn.ExecuteAsync(new PostGrnCommand(
            SelectedPurchaseOrder?.Id,
            supplierId,
            VendorInvoiceNumber,
            invoiceDate,
            GrnNotes,
            _currentUser.UserId,
            null,
            lines));

        StatusMessage = result.IsSuccess
            ? $"GRN {result.Value.Number} posted."
            : result.Error ?? "GRN posting failed.";

        if (result.IsSuccess)
        {
            GrnLines.Clear();
            VendorInvoiceNumber = string.Empty;
            VendorInvoiceDateText = string.Empty;
            GrnNotes = string.Empty;

            await RefreshActivePurchaseOrdersAsync();
        }
    }

    private static decimal ParseDecimal(string value)
    {
        return decimal.TryParse(value, out var result) ? result : 0m;
    }

    private static DateTime? ParseNullableDate(string value)
    {
        return DateTime.TryParse(value, out var result) ? result : null;
    }
}