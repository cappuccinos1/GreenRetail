namespace GreenRetail.Accounting;

public enum AccountType
{
    Asset = 1,
    Liability = 2,
    Equity = 3,
    Revenue = 4,
    Expense = 5
}

public enum NormalBalance
{
    Debit = 1,
    Credit = 2
}

public enum JournalStatus
{
    Draft = 0,
    Posted = 1,
    Void = 2
}

public enum PeriodStatus
{
    Open = 0,
    Closed = 1
}

public enum AccountingSourceType
{
    Manual = 0,
    OpeningBalance = 1,
    Sale = 2,
    Refund = 3,
    StoreCreditIssued = 4,
    StoreCreditRedeemed = 5,
    CashDeposit = 6,
    Expense = 7,
    PurchaseReceipt = 8,
    SupplierPayment = 9,
    CustomerReceipt = 10,
    InventoryAdjustment = 11,
    TransferLoss = 12,
    Tax = 13,
    Payroll = 14,
    BankFee = 15,
    Other = 16
}

public static class SystemAccountKeys
{
    public const string Cash = "CASH";
    public const string Bank = "BANK";
    public const string CardReceivable = "CARD_RECEIVABLE";
    public const string MobileMoneyReceivable = "MOBILE_MONEY_RECEIVABLE";
    public const string Inventory = "INVENTORY";
    public const string SupplierPayable = "SUPPLIER_PAYABLE";
    public const string StoreCreditLiability = "STORE_CREDIT_LIABILITY";
    public const string TaxPayable = "TAX_PAYABLE";
    public const string SalesRevenue = "SALES_REVENUE";
    public const string SalesReturns = "SALES_RETURNS";
    public const string CostOfGoodsSold = "COST_OF_GOODS_SOLD";
    public const string InventoryShrinkage = "INVENTORY_SHRINKAGE";
    public const string TransitLoss = "TRANSIT_LOSS";
    public const string OpeningBalanceEquity = "OPENING_BALANCE_EQUITY";
}

public class Account
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;

    public AccountType Type { get; set; }
    public string SubType { get; set; } = string.Empty;

    public Guid? ParentId { get; set; }
    public Account? Parent { get; set; }

    public NormalBalance NormalBalance { get; set; }

    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;
    public bool IsSystem { get; set; }

    public bool IsCashAccount { get; set; }
    public bool IsBankAccount { get; set; }
    public bool IsTaxAccount { get; set; }

    public DateTime CreatedUtc { get; set; }

    public List<Account> Children { get; set; } = new();
}

public class FiscalPeriod
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public int Year { get; set; }
    public int PeriodNumber { get; set; }

    public string Name { get; set; } = string.Empty;

    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }

    public PeriodStatus Status { get; set; } = PeriodStatus.Open;

    public bool IsYearEnd { get; set; }

    public DateTime CreatedUtc { get; set; }
}

public class Journal
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Number { get; set; } = string.Empty;

    public DateTime Date { get; set; }

    public string? Reference { get; set; }

    public AccountingSourceType SourceType { get; set; }
    public Guid? SourceId { get; set; }

    public string? Memo { get; set; }

    public JournalStatus Status { get; set; } = JournalStatus.Draft;

    public Guid? BranchId { get; set; }

    public Guid? CreatedByUserId { get; set; }
    public Guid? PostedByUserId { get; set; }

    public DateTime? PostedUtc { get; set; }
    public DateTime CreatedUtc { get; set; }

    public List<JournalLine> Lines { get; set; } = new();
}

public class JournalLine
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid JournalId { get; set; }
    public Journal? Journal { get; set; }

    public Guid AccountId { get; set; }
    public Account? Account { get; set; }

    public long DebitKobo { get; set; }
    public long CreditKobo { get; set; }

    public string? Memo { get; set; }

    public Guid? BranchId { get; set; }

    public string? EntityType { get; set; }
    public Guid? EntityId { get; set; }
}

public class JournalSequence
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Prefix { get; set; } = string.Empty;
    public long LastNumber { get; set; }
}

public class SystemAccountSetting
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Key { get; set; } = string.Empty;

    public Guid AccountId { get; set; }
    public Account? Account { get; set; }

    public string? Description { get; set; }
}