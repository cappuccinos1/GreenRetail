using Microsoft.EntityFrameworkCore;
using GreenRetail.Core.Abstractions;
using GreenRetail.Core.Results;
using GreenRetail.Data;
using GreenRetail.Data.Entities;

namespace GreenRetail.Refunds;

public sealed record IssueStoreCreditCommand(
    long AmountKobo,
    string? CustomerName,
    string? CustomerPhone,
    Guid? SourceSaleId,
    Guid? SourceRefundId);

public sealed record StoreCreditVoucherResult(
    Guid VoucherId,
    string Code,
    long AmountKobo,
    long BalanceKobo);

public interface IIssueStoreCreditVoucherUseCase
    : IUseCase<IssueStoreCreditCommand, Result<StoreCreditVoucherResult>>
{
}

public sealed class IssueStoreCreditVoucherUseCase : IIssueStoreCreditVoucherUseCase
{
    private readonly IDbContextFactory<PosDbContext> _dbContextFactory;
    private readonly IClock _clock;

    public IssueStoreCreditVoucherUseCase(
        IDbContextFactory<PosDbContext> dbContextFactory,
        IClock clock)
    {
        _dbContextFactory = dbContextFactory;
        _clock = clock;
    }

    public async Task<Result<StoreCreditVoucherResult>> ExecuteAsync(
        IssueStoreCreditCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.AmountKobo <= 0)
        {
            return Result<StoreCreditVoucherResult>.Fail("Store credit amount must be greater than zero.");
        }

        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        var voucher = new StoreCreditVoucher
        {
            Code = GenerateVoucherCode(),
            AmountKobo = command.AmountKobo,
            BalanceKobo = command.AmountKobo,
            Status = StoreCreditVoucherStatus.Active,
            IssuedUtc = _clock.UtcNow,
            SourceSaleId = command.SourceSaleId,
            SourceRefundId = command.SourceRefundId,
            CustomerName = command.CustomerName,
            CustomerPhone = command.CustomerPhone
        };

        db.StoreCreditVouchers.Add(voucher);
        await db.SaveChangesAsync(cancellationToken);

        return Result<StoreCreditVoucherResult>.Ok(new StoreCreditVoucherResult(
            voucher.Id,
            voucher.Code,
            voucher.AmountKobo,
            voucher.BalanceKobo));
    }

    private static string GenerateVoucherCode()
    {
        return Guid.NewGuid()
            .ToString("N")
            .Substring(0, 12)
            .ToUpperInvariant();
    }
}

public sealed record RedeemStoreCreditCommand(
    string Code,
    long AmountKobo,
    Guid? SaleId);

public interface IRedeemStoreCreditVoucherUseCase
    : IUseCase<RedeemStoreCreditCommand, Result<StoreCreditVoucherResult>>
{
}

public sealed class RedeemStoreCreditVoucherUseCase : IRedeemStoreCreditVoucherUseCase
{
    private readonly IDbContextFactory<PosDbContext> _dbContextFactory;
    private readonly IClock _clock;

    public RedeemStoreCreditVoucherUseCase(
        IDbContextFactory<PosDbContext> dbContextFactory,
        IClock clock)
    {
        _dbContextFactory = dbContextFactory;
        _clock = clock;
    }

    public async Task<Result<StoreCreditVoucherResult>> ExecuteAsync(
        RedeemStoreCreditCommand command,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.Code))
        {
            return Result<StoreCreditVoucherResult>.Fail("Voucher code is required.");
        }

        if (command.AmountKobo <= 0)
        {
            return Result<StoreCreditVoucherResult>.Fail("Redemption amount must be greater than zero.");
        }

        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        var code = command.Code.Trim().ToUpperInvariant();

        var voucher = await db.StoreCreditVouchers
            .FirstOrDefaultAsync(x => x.Code == code, cancellationToken);

        if (voucher is null)
        {
            return Result<StoreCreditVoucherResult>.Fail("Voucher not found.");
        }

        if (voucher.Status != StoreCreditVoucherStatus.Active)
        {
            return Result<StoreCreditVoucherResult>.Fail("Voucher is not active.");
        }

        if (voucher.BalanceKobo < command.AmountKobo)
        {
            return Result<StoreCreditVoucherResult>.Fail("Voucher balance is insufficient.");
        }

        voucher.BalanceKobo -= command.AmountKobo;

        if (voucher.BalanceKobo == 0)
        {
            voucher.Status = StoreCreditVoucherStatus.Redeemed;
        }

        db.StoreCreditRedemptions.Add(new StoreCreditRedemption
        {
            VoucherId = voucher.Id,
            AmountKobo = command.AmountKobo,
            SaleId = command.SaleId,
            RedeemedUtc = _clock.UtcNow
        });

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return Result<StoreCreditVoucherResult>.Ok(new StoreCreditVoucherResult(
            voucher.Id,
            voucher.Code,
            voucher.AmountKobo,
            voucher.BalanceKobo));
    }
}

public sealed record InitiateRefundRequestCommand(
    Guid? SaleId,
    Guid? CashierManagerId,
    string Reason,
    long TotalKobo);

public sealed record RefundRequestResult(
    Guid RefundRequestId,
    RefundStatus Status);

public interface IInitiateRefundRequestUseCase
    : IUseCase<InitiateRefundRequestCommand, Result<RefundRequestResult>>
{
}

public sealed class InitiateRefundRequestUseCase : IInitiateRefundRequestUseCase
{
    private readonly IDbContextFactory<PosDbContext> _dbContextFactory;
    private readonly IClock _clock;

    public InitiateRefundRequestUseCase(
        IDbContextFactory<PosDbContext> dbContextFactory,
        IClock clock)
    {
        _dbContextFactory = dbContextFactory;
        _clock = clock;
    }

    public async Task<Result<RefundRequestResult>> ExecuteAsync(
        InitiateRefundRequestCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.TotalKobo <= 0)
        {
            return Result<RefundRequestResult>.Fail("Refund amount must be greater than zero.");
        }

        if (string.IsNullOrWhiteSpace(command.Reason))
        {
            return Result<RefundRequestResult>.Fail("Refund reason is required.");
        }

        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        var refund = new RefundRequest
        {
            SaleId = command.SaleId,
            CashierManagerId = command.CashierManagerId,
            Reason = command.Reason,
            TotalKobo = command.TotalKobo,
            Status = RefundStatus.Requested,
            CreatedUtc = _clock.UtcNow
        };

        db.RefundRequests.Add(refund);
        await db.SaveChangesAsync(cancellationToken);

        return Result<RefundRequestResult>.Ok(new RefundRequestResult(
            refund.Id,
            refund.Status));
    }
}

public sealed record ApproveRefundRequestCommand(
    Guid RefundRequestId,
    string? CustomerName,
    string? CustomerPhone);

public interface IApproveRefundRequestUseCase
    : IUseCase<ApproveRefundRequestCommand, Result<StoreCreditVoucherResult>>
{
}

public sealed class ApproveRefundRequestUseCase : IApproveRefundRequestUseCase
{
    private readonly IDbContextFactory<PosDbContext> _dbContextFactory;
    private readonly IClock _clock;

    public ApproveRefundRequestUseCase(
        IDbContextFactory<PosDbContext> dbContextFactory,
        IClock clock)
    {
        _dbContextFactory = dbContextFactory;
        _clock = clock;
    }

    public async Task<Result<StoreCreditVoucherResult>> ExecuteAsync(
        ApproveRefundRequestCommand command,
        CancellationToken cancellationToken = default)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        var refund = await db.RefundRequests
            .FirstOrDefaultAsync(x => x.Id == command.RefundRequestId, cancellationToken);

        if (refund is null)
        {
            return Result<StoreCreditVoucherResult>.Fail("Refund request not found.");
        }

        if (refund.Status != RefundStatus.Requested)
        {
            return Result<StoreCreditVoucherResult>.Fail("Refund request has already been processed.");
        }

        var voucher = new StoreCreditVoucher
        {
            Code = GenerateVoucherCode(),
            AmountKobo = refund.TotalKobo,
            BalanceKobo = refund.TotalKobo,
            Status = StoreCreditVoucherStatus.Active,
            IssuedUtc = _clock.UtcNow,
            SourceSaleId = refund.SaleId,
            SourceRefundId = refund.Id,
            CustomerName = command.CustomerName,
            CustomerPhone = command.CustomerPhone
        };

        db.StoreCreditVouchers.Add(voucher);

        refund.Status = RefundStatus.Completed;
        refund.StoreCreditVoucherId = voucher.Id;
        refund.CompletedUtc = _clock.UtcNow;

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return Result<StoreCreditVoucherResult>.Ok(new StoreCreditVoucherResult(
            voucher.Id,
            voucher.Code,
            voucher.AmountKobo,
            voucher.BalanceKobo));
    }

    private static string GenerateVoucherCode()
    {
        return Guid.NewGuid()
            .ToString("N")
            .Substring(0, 12)
            .ToUpperInvariant();
    }
}