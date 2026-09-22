using GreenRetail.Data.Entities;

namespace GreenRetail.Procurement;

public enum ReceivingStatus
{
    InInspection,
    ReadyForPosting,
    Posted,
    Cancelled
}

/// <summary>
/// Operational receiving/Quality Control staging record. It is deliberately separate from the
/// posted GRN so Quality Control can inspect goods without changing inventory balances.
/// </summary>
public class ReceivingSession
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Number { get; set; } = string.Empty;

    public Guid? PurchaseOrderId { get; set; }
    public PurchaseOrder? PurchaseOrder { get; set; }

    public Guid SupplierId { get; set; }
    public Supplier? Supplier { get; set; }

    public Guid BranchId { get; set; }
    public Branch? Branch { get; set; }

    public string VendorInvoiceNumber { get; set; } = string.Empty;
    public DateTime VendorInvoiceDate { get; set; }
    public string? Notes { get; set; }

    public ReceivingStatus Status { get; set; } = ReceivingStatus.InInspection;

    public DateTime ReceivedUtc { get; set; }
    public Guid ReceivedByUserId { get; set; }
    public DateTime? InspectionCompletedUtc { get; set; }
    public Guid? InspectedByUserId { get; set; }
    public DateTime? PostedUtc { get; set; }
    public Guid? PostedByUserId { get; set; }

    // A no-PO transaction must be explicitly confirmed by a buying officer.
    public DateTime? NoPoBuyerConfirmedUtc { get; set; }
    public Guid? NoPoBuyerConfirmedByUserId { get; set; }

    public List<ReceivingLine> Lines { get; set; } = new();
}

public class ReceivingLine
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid ReceivingSessionId { get; set; }
    public ReceivingSession? ReceivingSession { get; set; }

    public Guid ProductId { get; set; }
    public Product? Product { get; set; }

    public decimal DeliveredQuantity { get; set; }
    public decimal AcceptedQuantity { get; set; }
    public decimal RejectedQuantity { get; set; }

    // Cost is stored in the canonical minor-unit representation for new financial data.
    public long UnitCostKobo { get; set; }

    public string? RejectReason { get; set; }
    public DateTime? ExpiryUtc { get; set; }
    public string? BatchNumber { get; set; }
}
