using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using GreenRetail.Core.Abstractions;
using GreenRetail.Core.Pricing;
using GreenRetail.Core.Results;
using GreenRetail.Core.ValueObjects;
using GreenRetail.Data;
using GreenRetail.Data.Entities;

namespace GreenRetail.Features.Checkout;

public sealed record CompleteSaleItem(
    Guid ProductId,
    decimal Quantity);

public sealed record CompleteSalePayment(
    PaymentMethod Method,
    Money Amount,
    string? Reference);

public sealed record CompleteSaleCommand(
    Guid? CustomerId,
    Guid? CashierId,
    string CashierName,
    IReadOnlyList<CompleteSaleItem> Items,
    IReadOnlyList<CompleteSalePayment> Payments);

public sealed record CompletedSale(
    Guid SaleId,
    Money Total,
    Money Tendered,
    Money ChangeDue,
    Money BalanceDue);

public interface ICompleteSaleUseCase
    : IUseCase<CompleteSaleCommand, Result<CompletedSale>>
{
}

public sealed class CompleteSaleUseCase : ICompleteSaleUseCase
{
    private readonly IDbContextFactory<PosDbContext> _dbContextFactory;
    private readonly IPricingPolicy _pricingPolicy;
    private readonly IClock _clock;

    public CompleteSaleUseCase(
        IDbContextFactory<PosDbContext> dbContextFactory,
        IPricingPolicy pricingPolicy,
        IClock clock)
    {
        _dbContextFactory = dbContextFactory;
        _pricingPolicy = pricingPolicy;
        _clock = clock;
    }

    public async Task<Result<CompletedSale>> ExecuteAsync(
        CompleteSaleCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.Items.Count == 0)
        {
            return Result<CompletedSale>.Fail("Cart is empty.");
        }

        if (command.Items.Any(x => x.Quantity <= 0m))
        {
            return Result<CompletedSale>.Fail("Quantity must be greater than zero.");
        }

        if (command.Payments.Any(x => x.Amount.Kobo < 0))
        {
            return Result<CompletedSale>.Fail("Payment amount cannot be negative.");
        }

        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var productIds = command.Items
                .Select(x => x.ProductId)
                .Distinct()
                .ToList();

            var products = await db.Products
                .Where(x => productIds.Contains(x.Id))
                .ToListAsync(cancellationToken);

            if (products.Count != productIds.Count)
            {
                return Result<CompletedSale>.Fail("One or more products could not be found.");
            }

            var productLookup = products.ToDictionary(x => x.Id);

            var stockLevels = await db.StockLevels
                .Where(x => productIds.Contains(x.ProductId))
                .ToListAsync(cancellationToken);

            var stockLookup = stockLevels.ToDictionary(x => x.ProductId);

            var sale = new Sale
            {
                CustomerId = command.CustomerId,
                CashierId = command.CashierId,
                CashierName = command.CashierName,
                Status = SaleStatus.Completed,
                CreatedUtc = _clock.UtcNow
            };

            var subtotal = Money.Zero;

            foreach (var item in command.Items)
            {
                var product = productLookup[item.ProductId];

                var unitPrice = Money.FromNaira(product.SellingPrice);
                var lineTotal = unitPrice * item.Quantity;

                subtotal += lineTotal;

                sale.Items.Add(new SaleItem
                {
                    ProductId = product.Id,
                    Quantity = item.Quantity,
                    UnitPriceKobo = unitPrice.Kobo,
                    TotalKobo = lineTotal.Kobo
                });

                if (product.TrackStock)
                {
                    if (!stockLookup.TryGetValue(product.Id, out var stock))
                    {
                        stock = new StockLevel
                        {
                            ProductId = product.Id,
                            Quantity = 0m
                        };

                        db.StockLevels.Add(stock);
                        stockLookup[product.Id] = stock;
                    }

                    var newQuantity = stock.Quantity - item.Quantity;

                    if (newQuantity < 0m && !product.AllowNegativeStock)
                    {
                        return Result<CompletedSale>.Fail($"Insufficient stock for {product.Name}.");
                    }

                    stock.Quantity = newQuantity;

                    db.StockLedger.Add(new StockLedgerEntry
                    {
                        ProductId = product.Id,
                        QuantityChange = -item.Quantity,
                        Reason = StockMovementReason.Sale,
                        Note = "POS sale",
                        CreatedUtc = _clock.UtcNow
                    });
                }
            }

            var total = _pricingPolicy.RoundTotal(subtotal);

            sale.SubtotalKobo = subtotal.Kobo;
            sale.RoundingKobo = (total - subtotal).Kobo;
            sale.TotalKobo = total.Kobo;

            var session = await db.CashSessions
                .FirstOrDefaultAsync(x => x.IsOpen, cancellationToken);

            if (session is null)
            {
                session = new CashSession
                {
                    CashierId = command.CashierId,
                    CashierName = command.CashierName,
                    OpeningCash = 0m,
                    ExpectedCash = 0m,
                    IsOpen = true,
                    OpenedUtc = _clock.UtcNow
                };

                db.CashSessions.Add(session);
            }

            sale.CashSessionId = session.Id;

            var remaining = total;
            var tendered = Money.Zero;

            foreach (var payment in command.Payments)
            {
                tendered += payment.Amount;

                var applied = payment.Amount > remaining
                    ? remaining
                    : payment.Amount;

                if (applied.Kobo > 0)
                {
                    sale.Payments.Add(new Payment
                    {
                        Method = payment.Method,
                        AmountKobo = applied.Kobo,
                        Reference = payment.Reference,
                        CreatedUtc = _clock.UtcNow
                    });

                    remaining -= applied;
                }
            }

            sale.TenderedKobo = tendered.Kobo;
            sale.PaidKobo = (total - remaining).Kobo;
            sale.BalanceDueKobo = remaining.Kobo;
            sale.ChangeDueKobo = _pricingPolicy.CalculateChange(total, tendered).Kobo;

            var cashAppliedKobo = sale.Payments
                .Where(x => x.Method == PaymentMethod.Cash)
                .Sum(x => x.AmountKobo);

            session.ExpectedCash += cashAppliedKobo / 100m;

            db.Sales.Add(sale);

            db.SyncOutbox.Add(new SyncOutbox
            {
                EntityType = nameof(Sale),
                EntityId = sale.Id,
                Operation = "Create",
                CreatedUtc = _clock.UtcNow,
                PayloadJson = JsonSerializer.Serialize(new
                {
                    SaleId = sale.Id,
                    Total = Money.FromNaira(sale.TotalKobo / 100m),
                    Paid = Money.FromNaira(sale.PaidKobo / 100m),
                    BalanceDue = Money.FromNaira(sale.BalanceDueKobo / 100m),
                    ChangeDue = Money.FromNaira(sale.ChangeDueKobo / 100m),
                    CreatedUtc = sale.CreatedUtc
                })
            });

            db.AuditLog.Add(new AuditLogEntry
            {
                CreatedUtc = _clock.UtcNow,
                UserId = command.CashierId,
                Action = "SaleCompleted",
                Details = $"Sale {sale.Id} completed. Total {Money.FromNaira(sale.TotalKobo / 100m)}."
            });

            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return Result<CompletedSale>.Ok(new CompletedSale(
                sale.Id,
                Money.FromNaira(sale.TotalKobo / 100m),
                Money.FromNaira(sale.TenderedKobo / 100m),
                Money.FromNaira(sale.ChangeDueKobo / 100m),
                Money.FromNaira(sale.BalanceDueKobo / 100m)));
        }
        catch (DbUpdateException)
        {
            return Result<CompletedSale>.Fail("Database error while saving the sale.");
        }
    }
}