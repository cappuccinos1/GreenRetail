namespace GreenRetail.Data.Entities;

public enum CashSessionStatus
{
    Open,
    Suspended,
    Counting,
    Closed
}

public class CashSession
{
    public Guid Id { get; set; } = Guid.NewGuid();

    // P0-5: Terminal/Register Scoping
    public Guid TerminalId { get; set; }
    public Terminal? Terminal { get; set; }

    public Guid BranchId { get; set; }
    public Branch? Branch { get; set; }

    public Guid CashierId { get; set; }
    public AppUser? Cashier { get; set; }
    public string CashierName { get; set; } = string.Empty;

    public CashSessionStatus Status { get; set; } = CashSessionStatus.Open;

    // Financials in Kobo
    public long OpeningCashKobo { get; set; }
    
    // System calculated expected cash (Opening + Cash Sales - Cash Payouts)
    public long ExpectedCashKobo { get; set; } 

    // Blind count entered by Accounts staff
    public long? CountedCashKobo { get; set; }
    public long? VarianceKobo { get; set; }

    // Audit trail for who counted and approved the close
    public Guid? CountedByUserId { get; set; }
    public AppUser? CountedByUser { get; set; }

    public DateTime OpenedUtc { get; set; }
    public DateTime? ClosedUtc { get; set; }
}