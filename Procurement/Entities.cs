using GreenRetail.Data.Entities;

namespace GreenRetail.Procurement;

public enum PurchaseOrderStatus
{
    Draft,
    Active,
    Closed,
    Cancelled
}

public enum GrnStatus
{
    Draft,
    Posted,
    Cancelled
}

public class Supplier
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedUtc { get; set; }
}

public class PurchaseOrder
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Number { get; set; } = string.Empty;

    public Guid SupplierId { get; set; }
    public Supplier? Supplier { get; set; }

    public Guid? BranchId { get; set; }

    public string? Notes { get; set; }

    public PurchaseOrderStatus Status { get; set; } = PurchaseOrderStatus.Active;

    public decimal TotalAmount { get; set; }

    public DateTime OrderDateUtc { get; set; }
    public DateTime? ClosedUtc { get; set; }

    public Guid? CreatedByUserId { get; set; }

    public List<PurchaseOrderLine> Lines { get; set; } = new();
}

public class PurchaseOrderLine
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid PurchaseOrderId { get; set; }
    public PurchaseOrder? PurchaseOrder { get; set; }

    public Guid ProductId { get; set; }
    public Product? Product { get; set; }

    public decimal OrderedQuantity { get; set; }
    public decimal UnitCost { get; set; }
}

public class GoodsReceivedNote
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Number { get; set; } = string.Empty;

    public Guid? PurchaseOrderId { get; set; }
    public PurchaseOrder? PurchaseOrder { get; set; }

    public Guid SupplierId { get; set; }
    public Supplier? Supplier { get; set; }

    public string VendorInvoiceNumber { get; set; } = string.Empty;
    public DateTime VendorInvoiceDate { get; set; }

    public Guid? BranchId { get; set; }

    public GrnStatus Status { get; set; } = GrnStatus.Posted;

    public string? Notes { get; set; }

    public DateTime ReceivedUtc { get; set; }
    public DateTime? PostedUtc { get; set; }

    public Guid? PostedByUserId { get; set; }

    public List<GoodsReceivedNoteLine> Lines { get; set; } = new();
}

public class GoodsReceivedNoteLine
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid GoodsReceivedNoteId { get; set; }
    public GoodsReceivedNote? GoodsReceivedNote { get; set; }

    public Guid ProductId { get; set; }
    public Product? Product { get; set; }

    public decimal OrderedQuantity { get; set; }
    public decimal AcceptedQuantity { get; set; }
    public decimal RejectedQuantity { get; set; }

    public string? RejectReason { get; set; }

    public DateTime? ExpiryUtc { get; set; }
    public string? BatchNumber { get; set; }
}