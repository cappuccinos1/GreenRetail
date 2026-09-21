namespace GreenRetail.Data.Entities;

public enum StockMovementReason
{
    Sale, Purchase, CustomerReturn, SupplierReturn, Adjustment, Damage, StockTake, TransferIn, TransferOut
}

public class Branch
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedUtc { get; set; }
}

public class AppUser
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string UserName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Role { get; set; } = "Cashier";
    public byte[] PasswordSalt { get; set; } = Array.Empty<byte>();
    public byte[] PasswordHash { get; set; } = Array.Empty<byte>();
    public bool IsActive { get; set; } = true;
    public bool RequiresPasswordChange { get; set; }
    public int FailedLoginCount { get; set; }
    public DateTime? LockoutEndUtc { get; set; }
    public DateTime CreatedUtc { get; set; }
}

public class Unit
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Abbreviation { get; set; } = string.Empty;
    public bool IsWeighed { get; set; }
}

public class Category
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public bool IsPharmacy { get; set; }
}

public class Product
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public Guid CategoryId { get; set; }
    public Category? Category { get; set; }
    public Guid UnitId { get; set; }
    public Unit? Unit { get; set; }
    public decimal CostPrice { get; set; }
    public decimal SellingPrice { get; set; }
    public bool TrackStock { get; set; } = true;
    public bool AllowNegativeStock { get; set; } = true;
    public bool IsWeighed { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? ExpiryUtc { get; set; }
    public bool RequiresExpiry { get; set; }
    public bool RequiresBatch { get; set; }
    public bool RefundAllowed { get; set; } = true;
    public bool Perishable { get; set; }
    public bool RequiresSecurityReturnCheck { get; set; }
    public List<Barcode> Barcodes { get; set; } = new();
}

public class Barcode
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProductId { get; set; }
    public Product? Product { get; set; }
    public string Value { get; set; } = string.Empty;
}

public class StockLevel
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProductId { get; set; }
    public Product? Product { get; set; }
    public decimal Quantity { get; set; }
}

public class StockLedgerEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProductId { get; set; }
    public Product? Product { get; set; }
    public decimal QuantityChange { get; set; }
    public StockMovementReason Reason { get; set; }
    public string? Note { get; set; }
    public DateTime CreatedUtc { get; set; }
}

public class Customer
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? WhatsApp { get; set; }
    public decimal CreditLimit { get; set; }
    public decimal Balance { get; set; }
}

public class SyncOutbox
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string EntityType { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public string Operation { get; set; } = string.Empty;
    public string PayloadJson { get; set; } = string.Empty;
    public DateTime CreatedUtc { get; set; }
    public DateTime? ProcessedUtc { get; set; }
    public int RetryCount { get; set; }
    public string? LastError { get; set; }
}

public class AuditLogEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime CreatedUtc { get; set; }
    public Guid? UserId { get; set; }
    public AppUser? User { get; set; }
    public string Action { get; set; } = string.Empty;
    public string? Details { get; set; }
}