using Microsoft.EntityFrameworkCore;
using GreenRetail.Core.Abstractions;
using GreenRetail.Core.Results;
using GreenRetail.Core.Terminal;
using GreenRetail.Data;
using GreenRetail.Data.Entities;
using GreenRetail.Accounting;

namespace GreenRetail.Features.Checkout;

public sealed record CreatedSaleResult(Guid SaleId, string ReceiptNumber, long TotalKobo);

public interface ICreateSaleUseCase : IUseCase<CreateSaleCommand, Result<CreatedSaleResult>> { }

public sealed class CreateSaleUseCase : ICreateSaleUseCase
{
    private readonly IDbContextFactory<PosDbContext> _dbContextFactory;
    private readonly ITerminalContext _terminalContext;
    private readonly IClock _clock;
    private readonly IAccountingPostingService _accountingPoster;

    public CreateSaleUseCase(
        IDbContextFactory<PosDbContext> dbContextFactory,
        ITerminalContext terminalContext,
        IClock clock,
        IAccountingPostingService accountingPoster)
    {
        _dbContextFactory = dbContextFactory;
        _terminalContext = terminalContext;
        _clock = clock;
        _accountingPoster = accountingPoster;
    }

    public async Task<Result<CreatedSaleResult>> ExecuteAsync(
        CreateSaleCommand command,
        CancellationToken cancellationToken = default)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        // 1. Idempotency Check
        var existingSale = await db.Sales
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.IdempotencyKey == command.IdempotencyKey, cancellationToken);

        if (existingSale != null)
        {
            return Result<CreatedSaleResult>.Ok(new CreatedSaleResult(
                existingSale.Id,
                existingSale.Id.ToString(),
                existingSale.TotalKobo));
        }

        // 2. Begin Atomic Transaction
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            // 3. Find active cash session
            var cashSession = await db.CashSessions
                .FirstOrDefaultAsync(x =>
                    x.TerminalId == _terminalContext.TerminalId &&
                    x.Status == CashSessionStatus.Open,
                    cancellationToken);

            // 4. Validate & Deduct Stock (Atomic)
            foreach (var item in command.Items)
            {
                var rowsAffected = await db.Database.ExecuteSqlRawAsync(
                    @"UPDATE StockLevels 
                      SET Quantity = Quantity - {0} 
                      WHERE ProductId = {1} 
                        AND (Quantity - {0}) >= 0",
                    item.Quantity,
                    item.ProductId,
                    cancellationToken);

                if (rowsAffected == 0)
                {
                    return Result<CreatedSaleResult>.Fail($"Insufficient stock for product {item.ProductId}.");
                }

                db.StockLedger.Add(new StockLedgerEntry
                {
                    ProductId = item.ProductId,
                    QuantityChange = -item.Quantity,
                    Reason = StockMovementReason.Sale,
                    Note = $"Sale {command.IdempotencyKey}",
                    CreatedUtc = _clock.UtcNow
                });
            }

            // 5. Create Sale Aggregate
            var sale = new Sale
            {
                IdempotencyKey = command.IdempotencyKey,
                TerminalId = _terminalContext.TerminalId,
                CashierId = command.CashierId,
                CashierName = command.CashierName,
                Status = SaleStatus.Completed,
                CreatedUtc = _clock.UtcNow,
                CashSessionId = cashSession?.Id
            };

            long subtotalKobo = 0;

            foreach (var item in command.Items)
            {
                var lineTotalKobo = item.UnitPriceKobo * (long)item.Quantity;
                subtotalKobo += lineTotalKobo;

                sale.Items.Add(new SaleItem
                {
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    UnitPriceKobo = item.UnitPriceKobo,
                    TotalKobo = lineTotalKobo
                });
            }

            sale.SubtotalKobo = subtotalKobo;
            sale.TotalKobo = subtotalKobo;

            long paidKobo = 0;
            foreach (var pmt in command.Payments)
            {
                paidKobo += pmt.AmountKobo;
                sale.Payments.Add(new Payment
                {
                    Method = pmt.Method,
                    AmountKobo = pmt.AmountKobo,
                    Status = PaymentStatus.Captured,
                    Reference = pmt.Reference,
                    CreatedUtc = _clock.UtcNow
                });
            }

            sale.PaidKobo = paidKobo;
            sale.TenderedKobo = paidKobo;
            sale.ChangeDueKobo = Math.Max(0, paidKobo - sale.TotalKobo);
            sale.BalanceDueKobo = Math.Max(0, sale.TotalKobo - paidKobo);

            // 6. Update cash session expected cash
            if (cashSession != null)
            {
                var cashPaymentKobo = sale.Payments
                    .Where(p => p.Method == PaymentMethod.Cash)
                    .Sum(p => p.AmountKobo);

                cashSession.ExpectedCashKobo += cashPaymentKobo;
            }

            db.Sales.Add(sale);
            
            // *** CRITICAL: Save Sale first to generate the ID needed for the Journal SourceId ***
            await db.SaveChangesAsync(cancellationToken);

            // 7. POST TO GENERAL LEDGER (Inside the same transaction!)
            await _accountingPoster.PostSaleAsync(db, sale, cancellationToken);

            // 8. Final Save (Commits Journal Lines)
            await db.SaveChangesAsync(cancellationToken);
            
            await transaction.CommitAsync(cancellationToken);

            return Result<CreatedSaleResult>.Ok(new CreatedSaleResult(
                sale.Id,
                sale.Id.ToString(),
                sale.TotalKobo));
        }
        catch (Exception ex)
        {
            // Transaction automatically rolls back on disposal if not committed
            return Result<CreatedSaleResult>.Fail($"Transaction failed: {ex.Message}");
        }
    }
}