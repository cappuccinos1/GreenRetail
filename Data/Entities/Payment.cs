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

public class Payment
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid SaleId { get; set; }
    public Sale? Sale { get; set; }

    public PaymentMethod Method { get; set; }
    
    // P1: Financial Precision (Kobo)
    public long AmountKobo { get; set; }

    public string? Reference { get; set; }
    public DateTime CreatedUtc { get; set; }
}