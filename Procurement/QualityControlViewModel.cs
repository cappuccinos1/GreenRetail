using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using GreenRetail.Core.Results;
using GreenRetail.Data;
using GreenRetail.Features.Catalog;
using GreenRetail.Rbac;
using GreenRetail.Shared.State;

namespace GreenRetail.Procurement;

public sealed record BranchChoice(Guid Id, string Code, string Name)
{
    public override string ToString() => $"{Code} — {Name}";
}

public sealed record PurchaseOrderChoice(Guid Id, string Number, Guid SupplierId, string SupplierName)
{
    public override string ToString() => $"{Number} — {SupplierName}";
}

public partial class ReceivingDraftLine : ObservableObject
{
    public Guid ProductId { get; init; }
    public string ProductName { get; init; } = string.Empty;

    [ObservableProperty] private string deliveredQuantity = string.Empty;
    [ObservableProperty] private string unitCostNaira = string.Empty;
}

public partial class QualityControlLineEditor : ObservableObject
{
    public Guid ReceivingLineId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public decimal DeliveredQuantity { get; init; }

    [ObservableProperty] private string acceptedQuantity = string.Empty;
    [ObservableProperty] private string rejectedQuantity = string.Empty;
    [ObservableProperty] private string rejectReason = string.Empty;
    [ObservableProperty] private string expiryDate = string.Empty;
    [ObservableProperty] private string batchNumber = string.Empty;
}

public partial class QualityControlViewModel : ObservableObject
{
    private readonly IDbContextFactory<PosDbContext> _dbFactory;
    private readonly ISearchProductsQuery _searchProducts;
    private readonly IGetSuppliersQuery _getSuppliers;
    private readonly IGetActivePurchaseOrdersQuery _getPurchaseOrders;
    private readonly IStartReceivingUseCase _startReceiving;
    private readonly ICompleteQualityControlUseCase _completeQualityControl;
    private readonly IConfirmNoPoReceivingUseCase _confirmNoPo;
    private readonly ICurrentUserService _currentUser;

    public ObservableCollection<BranchChoice> Branches { get; } = new();
    public ObservableCollection<SupplierListItem> Suppliers { get; } = new();
    public ObservableCollection<PurchaseOrderChoice> PurchaseOrders { get; } = new();
    public ObservableCollection<ProductSummary> Products { get; } = new();
    public ObservableCollection<ReceivingDraftLine> DraftLines { get; } = new();
    public ObservableCollection<QualityControlLineEditor> InspectionLines { get; } = new();

    [ObservableProperty] private BranchChoice? selectedBranch;
    [ObservableProperty] private SupplierListItem? selectedSupplier;
    [ObservableProperty] private PurchaseOrderChoice? selectedPurchaseOrder;
    [ObservableProperty] private ProductSummary? selectedProduct;
    [ObservableProperty] private string vendorInvoiceNumber = string.Empty;
    [ObservableProperty] private string vendorInvoiceDate = DateTime.Today.ToString("yyyy-MM-dd");
    [ObservableProperty] private string notes = string.Empty;
    [ObservableProperty] private string productSearch = string.Empty;
    [ObservableProperty] private string draftQuantity = string.Empty;
    [ObservableProperty] private string draftUnitCostNaira = string.Empty;
    [ObservableProperty] private string statusMessage = string.Empty;
    [ObservableProperty] private string activeReceivingNumber = string.Empty;
    [ObservableProperty] private Guid activeReceivingId;
    [ObservableProperty] private bool hasActiveReceiving;
    [ObservableProperty] private bool isBusy;

    public QualityControlViewModel(
        IDbContextFactory<PosDbContext> dbFactory,
        ISearchProductsQuery searchProducts,
        IGetSuppliersQuery getSuppliers,
        IGetActivePurchaseOrdersQuery getPurchaseOrders,
        IStartReceivingUseCase startReceiving,
        ICompleteQualityControlUseCase completeQualityControl,
        IConfirmNoPoReceivingUseCase confirmNoPo,
        ICurrentUserService currentUser)
    {
        _dbFactory = dbFactory;
        _searchProducts = searchProducts;
        _getSuppliers = getSuppliers;
        _getPurchaseOrders = getPurchaseOrders;
        _startReceiving = startReceiving;
        _completeQualityControl = completeQualityControl;
        _confirmNoPo = confirmNoPo;
        _currentUser = currentUser;
    }

    public async Task InitializeAsync()
    {
        try
        {
            IsBusy = true;
            await LoadBranchesAsync();
            await LoadSuppliersAsync();
            await SearchProductsAsync();
            await LoadPurchaseOrdersAsync();
            await LoadLatestInspectionAsync();
        }
        catch (Exception ex)
        {
            StatusMessage = "Quality Control could not load. See diagnostics for details.";
            App.LogCrash(ex);
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task SearchProductsAsync()
    {
        var result = await _searchProducts.ExecuteAsync(new SearchProductsQueryRequest(ProductSearch));
        if (!result.IsSuccess) { StatusMessage = result.Error ?? "Product search failed."; return; }
        Products.Clear();
        foreach (var item in result.Value) Products.Add(item);
    }

    private async Task LoadBranchesAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var rows = await db.Branches.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.Code).ToListAsync();
        Branches.Clear();
        foreach (var row in rows) Branches.Add(new BranchChoice(row.Id, row.Code, row.Name));
        SelectedBranch ??= Branches.FirstOrDefault();
    }

    private async Task LoadSuppliersAsync()
    {
        var result = await _getSuppliers.ExecuteAsync(new GetSuppliersQueryRequest(null));
        if (!result.IsSuccess) { StatusMessage = result.Error ?? "Supplier list could not be loaded."; return; }
        Suppliers.Clear();
        foreach (var item in result.Value) Suppliers.Add(item);
    }

    private async Task LoadPurchaseOrdersAsync()
    {
        var result = await _getPurchaseOrders.ExecuteAsync(new GetActivePurchaseOrdersQueryRequest(100));
        if (!result.IsSuccess) { StatusMessage = result.Error ?? "Purchase orders could not be loaded."; return; }
        PurchaseOrders.Clear();
        foreach (var item in result.Value)
            PurchaseOrders.Add(new PurchaseOrderChoice(item.Id, item.Number, item.SupplierId, item.SupplierName));
    }

    partial void OnSelectedSupplierChanged(SupplierListItem? value)
    {
        if (value is null) return;
        var po = PurchaseOrders.FirstOrDefault(x => x.SupplierId == value.Id);
        if (SelectedPurchaseOrder is not null && SelectedPurchaseOrder.SupplierId != value.Id)
            SelectedPurchaseOrder = null;
    }

    [RelayCommand]
    private void AddReceivingLine()
    {
        if (SelectedProduct is null)
        {
            StatusMessage = "Select a product first.";
            return;
        }

        if (!decimal.TryParse(DraftQuantity, NumberStyles.Number, CultureInfo.InvariantCulture, out var quantity) || quantity <= 0)
        {
            StatusMessage = "Enter a valid delivered quantity.";
            return;
        }

        if (!decimal.TryParse(DraftUnitCostNaira, NumberStyles.Number, CultureInfo.InvariantCulture, out var cost) || cost < 0)
        {
            StatusMessage = "Enter a valid unit cost.";
            return;
        }

        if (DraftLines.Any(x => x.ProductId == SelectedProduct.Id))
        {
            StatusMessage = "That product is already on this receiving transaction.";
            return;
        }

        DraftLines.Add(new ReceivingDraftLine
        {
            ProductId = SelectedProduct.Id,
            ProductName = SelectedProduct.Name,
            DeliveredQuantity = quantity.ToString(CultureInfo.InvariantCulture),
            UnitCostNaira = cost.ToString("0.00", CultureInfo.InvariantCulture)
        });

        DraftQuantity = string.Empty;
        DraftUnitCostNaira = string.Empty;
        StatusMessage = $"Added {SelectedProduct.Name}.";
    }

    [RelayCommand]
    private void RemoveReceivingLine(ReceivingDraftLine? line)
    {
        if (line is not null) DraftLines.Remove(line);
    }

    [RelayCommand]
    private async Task StartReceivingAsync()
    {
        if (!_currentUser.UserId.HasValue) { StatusMessage = "You are not signed in."; return; }
        if (SelectedBranch is null) { StatusMessage = "Select the receiving branch."; return; }
        if (SelectedSupplier is null) { StatusMessage = "Select the supplier."; return; }
        if (string.IsNullOrWhiteSpace(VendorInvoiceNumber)) { StatusMessage = "Vendor invoice number is required."; return; }
        if (!DateTime.TryParseExact(VendorInvoiceDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var invoiceDate))
        { StatusMessage = "Vendor invoice date must be yyyy-MM-dd."; return; }
        if (DraftLines.Count == 0) { StatusMessage = "Add at least one product line."; return; }

        var lines = new List<StartReceivingLineInput>();
        foreach (var line in DraftLines)
        {
            if (!decimal.TryParse(line.DeliveredQuantity, NumberStyles.Number, CultureInfo.InvariantCulture, out var qty) || qty <= 0 ||
                !decimal.TryParse(line.UnitCostNaira, NumberStyles.Number, CultureInfo.InvariantCulture, out var cost) || cost < 0)
            { StatusMessage = $"Invalid quantity or cost for {line.ProductName}."; return; }
            lines.Add(new StartReceivingLineInput(line.ProductId, qty, checked((long)Math.Round(cost * 100m, 0, MidpointRounding.AwayFromZero))));
        }

        try
        {
            IsBusy = true;
            var result = await _startReceiving.ExecuteAsync(new StartReceivingCommand(
                SelectedPurchaseOrder?.Id,
                SelectedSupplier.Id,
                SelectedBranch.Id,
                VendorInvoiceNumber.Trim(),
                invoiceDate,
                Notes,
                _currentUser.UserId.Value,
                lines));

            if (!result.IsSuccess) { StatusMessage = result.Error ?? "Receiving could not be started."; return; }
            ActiveReceivingId = result.Value.ReceivingSessionId;
            ActiveReceivingNumber = result.Value.Number;
            HasActiveReceiving = true;
            DraftLines.Clear();
            InspectionLines.Clear();
            StatusMessage = $"{result.Value.Number} is now awaiting Quality Control inspection.";
            await LoadInspectionAsync(ActiveReceivingId);
        }
        catch (Exception ex)
        {
            StatusMessage = "Receiving could not be started. See diagnostics for details.";
            App.LogCrash(ex);
        }
        finally { IsBusy = false; }
    }

    private async Task LoadLatestInspectionAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var session = await db.ReceivingSessions.AsNoTracking()
            .Where(x => x.Status == ReceivingStatus.InInspection || x.Status == ReceivingStatus.ReadyForPosting)
            .OrderByDescending(x => x.ReceivedUtc)
            .FirstOrDefaultAsync();
        if (session is not null)
        {
            ActiveReceivingId = session.Id;
            ActiveReceivingNumber = session.Number;
            HasActiveReceiving = session.Status == ReceivingStatus.InInspection;
            if (HasActiveReceiving) await LoadInspectionAsync(session.Id);
        }
    }

    private async Task LoadInspectionAsync(Guid id)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var session = await db.ReceivingSessions.AsNoTracking()
            .Include(x => x.Lines).ThenInclude(x => x.Product)
            .FirstOrDefaultAsync(x => x.Id == id);
        if (session is null) return;
        InspectionLines.Clear();
        foreach (var line in session.Lines)
            InspectionLines.Add(new QualityControlLineEditor
            {
                ReceivingLineId = line.Id,
                ProductName = line.Product?.Name ?? line.ProductId.ToString(),
                DeliveredQuantity = line.DeliveredQuantity,
                AcceptedQuantity = line.DeliveredQuantity.ToString(CultureInfo.InvariantCulture),
                RejectedQuantity = "0",
                ExpiryDate = DateTime.Today.AddDays(30).ToString("yyyy-MM-dd")
            });
    }

    [RelayCommand]
    private async Task CompleteQualityControlAsync()
    {
        if (!_currentUser.UserId.HasValue || !HasActiveReceiving) { StatusMessage = "There is no receiving transaction awaiting Quality Control."; return; }
        var inputs = new List<CompleteQualityControlLineInput>();
        foreach (var line in InspectionLines)
        {
            if (!decimal.TryParse(line.AcceptedQuantity, NumberStyles.Number, CultureInfo.InvariantCulture, out var accepted) ||
                !decimal.TryParse(line.RejectedQuantity, NumberStyles.Number, CultureInfo.InvariantCulture, out var rejected) ||
                !DateTime.TryParseExact(line.ExpiryDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var expiry))
            { StatusMessage = $"Enter valid Quality Control values for {line.ProductName}."; return; }
            inputs.Add(new CompleteQualityControlLineInput(line.ReceivingLineId, accepted, rejected, line.RejectReason, expiry, line.BatchNumber));
        }

        try
        {
            IsBusy = true;
            var result = await _completeQualityControl.ExecuteAsync(new CompleteQualityControlCommand(ActiveReceivingId, _currentUser.UserId.Value, inputs));
            if (!result.IsSuccess) { StatusMessage = result.Error ?? "Quality Control could not be completed."; return; }
            HasActiveReceiving = false;
            StatusMessage = $"{result.Value.Number} completed Quality Control and is staged for Inventory posting.";
        }
        catch (Exception ex)
        {
            StatusMessage = "Quality Control could not be completed. See diagnostics for details.";
            App.LogCrash(ex);
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task ConfirmNoPoAsync()
    {
        if (!_currentUser.UserId.HasValue || ActiveReceivingId == Guid.Empty) { StatusMessage = "No receiving transaction selected."; return; }
        var result = await _confirmNoPo.ExecuteAsync(new ConfirmNoPoReceivingCommand(ActiveReceivingId, _currentUser.UserId.Value));
        StatusMessage = result.IsSuccess ? "No-PO receiving confirmed by the current buying officer." : result.Error ?? "No-PO confirmation failed.";
    }
}
