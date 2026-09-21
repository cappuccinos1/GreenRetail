using GreenRetail.Data.Entities;

namespace GreenRetail.InventoryOps;

public enum StockAdjustmentStatus
{
    Requested,
    Approved,
    Rejected,
    Posted
}

public enum StockOverrideStatus
{
    Requested,
    Resolved,
    Rejected
}

public class StockAdjustmentRequest
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid ProductId { get; set; }
    public Product? Product { get; set; }

    public decimal QuantityChange { get; set; }

    public string Reason { get; set; } = string.Empty;
    public string? Note { get; set; }

    public StockAdjustmentStatus Status { get; set; } = StockAdjustmentStatus.Requested;

    public Guid? RequestedById { get; set; }
    public AppUser? RequestedBy { get; set; }

    public Guid? ApprovedById { get; set; }
    public AppUser? ApprovedBy { get; set; }

    public Guid? BranchId { get; set; }

    public DateTime CreatedUtc { get; set; }
    public DateTime? ProcessedUtc { get; set; }
}

public class StockOverrideRequest
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid ProductId { get; set; }
    public Product? Product { get; set; }

    public Guid? CashierId { get; set; }
    public AppUser? Cashier { get; set; }

    public Guid? BranchId { get; set; }

    public string? Reason { get; set; }

    public StockOverrideStatus Status { get; set; } = StockOverrideStatus.Requested;

    public Guid? ResolvedById { get; set; }
    public AppUser? ResolvedBy { get; set; }

    public DateTime CreatedUtc { get; set; }
    public DateTime? ResolvedUtc { get; set; }
}