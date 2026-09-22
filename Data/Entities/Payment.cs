namespace GreenRetail.Data.Entities;

public enum PaymentMethod
{
    Cash,
    BankTransfer,
    Card,
    PosTerminal,
    MobileMoney,
    CustomerCredit
}

public enum PaymentStatus
{
    Pending,    // E.g., waiting for bank transfer confirmation
    Captured,   // Successfully received/approved
    Failed,     // Declined or timed out
    Refunded,   // Reversed via refund process
    Voided      // Cancelled before settlement
}

public class Payment
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid SaleId { get; set; }
    public Sale? Sale { get; set; }

    public PaymentMethod Method { get; set; }
    public PaymentStatus Status { get; set; } = PaymentStatus.Captured;

    public long AmountKobo { get; set; }

    // Provider reference (e.g., POS terminal transaction ID, Bank Transfer Auth Code)
    public string? ProviderReference { get; set; }
    
    // Prevents duplicate payment processing
    public string? IdempotencyKey { get; set; }

    public string? Reference { get; set; }
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

    // Compatibility decimal property for older EF queries/use cases.
    public decimal Amount
    {
        get => AmountKobo / 100m;
        set => AmountKobo = (long)Math.Round(value * 100m, MidpointRounding.AwayFromZero);
    }
}