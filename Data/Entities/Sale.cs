namespace GreenRetail.Data.Entities;

public enum SaleStatus
{
    Open,
    Completed,
    Cancelled,
    Refunded
}

public class Sale
{
    public Guid Id { get; set; } = Guid.NewGuid();

    // P0-1: Idempotency Key prevents duplicate sales
    public string IdempotencyKey { get; set; } = string.Empty;

    // P0-5: Terminal Context
    public Guid TerminalId { get; set; }
    public Terminal? Terminal { get; set; }

    public Guid? CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public Guid? CashierId { get; set; }
    public AppUser? Cashier { get; set; }

    public Guid? CashSessionId { get; set; }
    public CashSession? CashSession { get; set; }

    public string CashierName { get; set; } = string.Empty;

    public SaleStatus Status { get; set; } = SaleStatus.Completed;

    // P1: Financial Precision (Kobo)
    public long SubtotalKobo { get; set; }
    public long DiscountKobo { get; set; }
    public long TaxKobo { get; set; }
    public long RoundingKobo { get; set; }

    public long TotalKobo { get; set; }
    public long PaidKobo { get; set; }
    public long TenderedKobo { get; set; }
    public long ChangeDueKobo { get; set; }
    public long BalanceDueKobo { get; set; }

    public DateTime CreatedUtc { get; set; }

    public List<SaleItem> Items { get; set; } = new();
    public List<Payment> Payments { get; set; } = new();
}

public class SaleItem
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid SaleId { get; set; }
    public Sale? Sale { get; set; }

    public Guid ProductId { get; set; }
    public Product? Product { get; set; }

    public decimal Quantity { get; set; }
    
    // P1: Financial Precision (Kobo)
    public long UnitPriceKobo { get; set; }
    public long DiscountKobo { get; set; }
    public long TotalKobo { get; set; }
}