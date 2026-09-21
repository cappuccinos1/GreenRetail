using GreenRetail.Data.Entities;

namespace GreenRetail.Refunds;

public enum RefundStatus
{
    Requested,
    Approved,
    Completed,
    Rejected
}

public enum StoreCreditVoucherStatus
{
    Active,
    Redeemed,
    Expired,
    Void
}

public class StoreCreditVoucher
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Code { get; set; } = string.Empty;

    public long AmountKobo { get; set; }
    public long BalanceKobo { get; set; }

    public StoreCreditVoucherStatus Status { get; set; } = StoreCreditVoucherStatus.Active;

    public DateTime IssuedUtc { get; set; }
    public DateTime? ExpiryUtc { get; set; }

    public Guid? SourceSaleId { get; set; }
    public Sale? SourceSale { get; set; }

    public Guid? SourceRefundId { get; set; }

    public string? CustomerName { get; set; }
    public string? CustomerPhone { get; set; }
}

public class StoreCreditRedemption
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid VoucherId { get; set; }
    public StoreCreditVoucher? Voucher { get; set; }

    public long AmountKobo { get; set; }

    public Guid? SaleId { get; set; }
    public Sale? Sale { get; set; }

    public DateTime RedeemedUtc { get; set; }
}

public class RefundRequest
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid? SaleId { get; set; }
    public Sale? Sale { get; set; }

    public Guid? CashierManagerId { get; set; }
    public AppUser? CashierManager { get; set; }

    public string Reason { get; set; } = string.Empty;

    public long TotalKobo { get; set; }

    public RefundStatus Status { get; set; } = RefundStatus.Requested;

    public Guid? StoreCreditVoucherId { get; set; }
    public StoreCreditVoucher? StoreCreditVoucher { get; set; }

    public DateTime CreatedUtc { get; set; }
    public DateTime? CompletedUtc { get; set; }
}