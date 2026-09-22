using Microsoft.EntityFrameworkCore;
using GreenRetail.Core.Abstractions;
using GreenRetail.Core.Results;
using GreenRetail.Core.Pricing;
using GreenRetail.Core.ValueObjects;
using GreenRetail.Core.Terminal;
using GreenRetail.Data;
using GreenRetail.Data.Entities;
using GreenRetail.Shared.State;
using GreenRetail.Rbac;
using Microsoft.Extensions.Logging;

namespace GreenRetail.Features.Checkout;

public sealed record CreatedSaleResult(
    Guid SaleId,
    string ReceiptNumber,
    long TotalKobo,
    long TenderedKobo,
    long ChangeDueKobo,
    long BalanceDueKobo);

public interface ICreateSaleUseCase : IUseCase<CreateSaleCommand, Result<CreatedSaleResult>> { }

/// <summary>
/// Authoritative sale transaction boundary.
/// The client may propose a price for display, but the server re-reads the product
/// price and stock state before committing the sale. This prevents a modified client
/// from selling an item below its current configured selling price.
/// </summary>
public sealed class CreateSaleUseCase : ICreateSaleUseCase
{
    private readonly IDbContextFactory<PosDbContext> _dbContextFactory;
    private readonly ITerminalContext _terminalContext;
    private readonly ICurrentUserService _currentUser;
    private readonly IPricingPolicy _pricingPolicy;
    private readonly IClock _clock;
    private readonly IAuthorizationService _authorization;
    private readonly ILogger<CreateSaleUseCase> _logger;

    public CreateSaleUseCase(
        IDbContextFactory<PosDbContext> dbContextFactory,
        ITerminalContext terminalContext,
        ICurrentUserService currentUser,
        IPricingPolicy pricingPolicy,
        IClock clock,
        IAuthorizationService authorization,
        ILogger<CreateSaleUseCase> logger)
    {
        _dbContextFactory = dbContextFactory;
        _terminalContext = terminalContext;
        _currentUser = currentUser;
        _pricingPolicy = pricingPolicy;
        _clock = clock;
        _authorization = authorization;
        _logger = logger;
    }

    public async Task<Result<CreatedSaleResult>> ExecuteAsync(
        CreateSaleCommand command,
        CancellationToken cancellationToken = default)
    {
        if (!_currentUser.IsAuthenticated || !_currentUser.UserId.HasValue)
            return Result<CreatedSaleResult>.Fail("You must be signed in to complete a sale.", ResultErrorCode.Authorization);

        if (!await _authorization.HasPermissionAsync(_currentUser.UserId.Value, PermissionCodes.PosSaleCreate, _terminalContext.BranchId, cancellationToken))
            return Result<CreatedSaleResult>.Fail("You do not have permission to create sales.", ResultErrorCode.Authorization);

        if (string.IsNullOrWhiteSpace(command.IdempotencyKey))
            return Result<CreatedSaleResult>.Fail("A sale idempotency key is required.");

        if (command.Items.Count == 0)
            return Result<CreatedSaleResult>.Fail("A sale must contain at least one item.");

        if (command.Items.Any(x => x.ProductId == Guid.Empty || x.Quantity <= 0m))
            return Result<CreatedSaleResult>.Fail("Sale quantities and products must be valid.");

        if (command.Payments.Count == 0 || command.Payments.Any(x => x.AmountKobo <= 0))
            return Result<CreatedSaleResult>.Fail("A sale must contain a valid payment.");

        if (command.Payments.Any(x => IsElectronic(x.Method) && string.IsNullOrWhiteSpace(x.Reference)))
            return Result<CreatedSaleResult>.Fail("Electronic payments require a transaction reference.");

        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        var existingSale = await db.Sales
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.IdempotencyKey == command.IdempotencyKey, cancellationToken);

        if (existingSale != null)
            return ToResult(existingSale);

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            // Re-check inside the transaction so retries cannot create a second sale.
            existingSale = await db.Sales
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.IdempotencyKey == command.IdempotencyKey, cancellationToken);

            if (existingSale != null)
                return ToResult(existingSale);

            var terminal = await db.Terminals
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == _terminalContext.TerminalId && x.IsActive, cancellationToken);

            if (terminal is null || terminal.BranchId is null)
                return Result<CreatedSaleResult>.Fail("This POS terminal is not configured with an active branch.", ResultErrorCode.Conflict);

            var cashSession = await db.CashSessions
                .FirstOrDefaultAsync(x =>
                    x.TerminalId == _terminalContext.TerminalId &&
                    x.Status == CashSessionStatus.Open,
                    cancellationToken);

            if (cashSession is null)
                return Result<CreatedSaleResult>.Fail("Open the register before completing a sale.");

            if (command.CashierId.HasValue && command.CashierId.Value != _currentUser.UserId.Value)
                return Result<CreatedSaleResult>.Fail("The sale cashier does not match the signed-in user.");

            var productIds = command.Items.Select(x => x.ProductId).Distinct().ToList();
            var products = await db.Products
                .Where(x => productIds.Contains(x.Id) && x.IsActive)
                .ToDictionaryAsync(x => x.Id, cancellationToken);

            if (products.Count != productIds.Count)
                return Result<CreatedSaleResult>.Fail("One or more sale items are no longer active.");

            long subtotalKobo = 0;
            var resolvedItems = new List<(CreateSaleItemCommand Command, Product Product, long UnitPriceKobo, long TotalKobo)>();

            foreach (var item in command.Items)
            {
                var product = products[item.ProductId];
                var unitPriceKobo = checked((long)Math.Round(product.SellingPrice * 100m, MidpointRounding.AwayFromZero));
                var lineTotalKobo = checked((long)Math.Round(unitPriceKobo * item.Quantity, MidpointRounding.AwayFromZero));

                subtotalKobo = checked(subtotalKobo + lineTotalKobo);
                resolvedItems.Add((item, product, unitPriceKobo, lineTotalKobo));
            }

            var totalKobo = _pricingPolicy.RoundTotal(new Money(subtotalKobo)).Kobo;
            var roundingKobo = checked(totalKobo - subtotalKobo);
            var paymentTotalKobo = checked(command.Payments.Sum(x => x.AmountKobo));
            var cashAppliedKobo = checked(command.Payments
                .Where(x => x.Method == PaymentMethod.Cash)
                .Sum(x => x.AmountKobo));
            var hasCustomerCredit = command.Payments.Any(x => x.Method == PaymentMethod.CustomerCredit);

            if (!hasCustomerCredit && paymentTotalKobo != totalKobo)
                return Result<CreatedSaleResult>.Fail("Payment total must exactly match the sale total.");

            if (hasCustomerCredit && paymentTotalKobo > totalKobo)
                return Result<CreatedSaleResult>.Fail("Payment total cannot exceed the sale total.");

            var cashTenderedKobo = cashAppliedKobo;
            if (cashAppliedKobo > 0)
            {
                cashTenderedKobo = command.CashTenderedKobo ?? cashAppliedKobo;
                if (cashTenderedKobo < cashAppliedKobo)
                    return Result<CreatedSaleResult>.Fail("Cash tendered cannot be less than the cash applied to the sale.");
            }
            else if ((command.CashTenderedKobo ?? 0) != 0)
            {
                return Result<CreatedSaleResult>.Fail("Cash tendered was supplied for a non-cash payment.");
            }

            var changeDueKobo = checked(cashTenderedKobo - cashAppliedKobo);
            var balanceDueKobo = checked(totalKobo - paymentTotalKobo);

            var sale = new Sale
            {
                IdempotencyKey = command.IdempotencyKey.Trim(),
                TerminalId = _terminalContext.TerminalId,
                CashierId = _currentUser.UserId.Value,
                CashierName = _currentUser.DisplayName ?? command.CashierName ?? "Unknown",
                Status = SaleStatus.Completed,
                CreatedUtc = _clock.UtcNow,
                CashSessionId = cashSession.Id,
                SubtotalKobo = subtotalKobo,
                RoundingKobo = roundingKobo,
                TotalKobo = totalKobo,
                PaidKobo = paymentTotalKobo,
                TenderedKobo = cashTenderedKobo,
                ChangeDueKobo = changeDueKobo,
                BalanceDueKobo = balanceDueKobo
            };

            foreach (var item in resolvedItems)
            {
                sale.Items.Add(new SaleItem
                {
                    ProductId = item.Product.Id,
                    Quantity = item.Command.Quantity,
                    UnitPriceKobo = item.UnitPriceKobo,
                    TotalKobo = item.TotalKobo
                });
            }

            foreach (var payment in command.Payments)
            {
                sale.Payments.Add(new Payment
                {
                    Method = payment.Method,
                    AmountKobo = payment.AmountKobo,
                    Status = PaymentStatus.Captured,
                    Reference = string.IsNullOrWhiteSpace(payment.Reference) ? null : payment.Reference.Trim(),
                    IdempotencyKey = payment.IdempotencyKey,
                    CreatedUtc = _clock.UtcNow
                });
            }

            foreach (var group in resolvedItems
                .Where(x => x.Product.TrackStock)
                .GroupBy(x => x.Product.Id)
                .Select(g => new { ProductId = g.Key, Quantity = g.Sum(x => x.Command.Quantity) }))
            {
                var rowsAffected = await db.Database.ExecuteSqlRawAsync(
                    @"UPDATE StockLevels
                      SET Quantity = Quantity - {0}
                      WHERE ProductId = {1}
                        AND BranchId = {2}
                        AND (Quantity - {0}) >= 0",
                    group.Quantity,
                    group.ProductId,
                    terminal.BranchId.Value,
                    cancellationToken);

                if (rowsAffected == 0)
                    return Result<CreatedSaleResult>.Fail($"Insufficient stock for product {group.ProductId}.");

                db.StockLedger.Add(new StockLedgerEntry
                {
                    ProductId = group.ProductId,
                    BranchId = terminal.BranchId.Value,
                    QuantityChange = -group.Quantity,
                    Reason = StockMovementReason.Sale,
                    Note = $"Sale {command.IdempotencyKey}",
                    CreatedUtc = _clock.UtcNow
                });
            }

            // Drawer movement is the cash actually applied to the sale. Change is
            // physically handed back and therefore must not inflate expected cash.
            cashSession.ExpectedCashKobo = checked(cashSession.ExpectedCashKobo + cashAppliedKobo);

            db.Sales.Add(sale);
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return Result<CreatedSaleResult>.Ok(ToCreatedResult(sale));
        }
        catch (DbUpdateException ex)
        {
            await transaction.RollbackAsync(CancellationToken.None);
            _logger.LogError(ex, "Database failure while creating sale {IdempotencyKey} for user {UserId}", command.IdempotencyKey, _currentUser.UserId);
            return Result<CreatedSaleResult>.Fail("The sale could not be saved. No money or stock was committed.", ResultErrorCode.Infrastructure);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(CancellationToken.None);
            _logger.LogError(ex, "Unexpected failure while creating sale {IdempotencyKey} for user {UserId}", command.IdempotencyKey, _currentUser.UserId);
            return Result<CreatedSaleResult>.Fail("The transaction could not be completed. Please try again.", ResultErrorCode.Unexpected);
        }
    }

    private static bool IsElectronic(PaymentMethod method) =>
        method is PaymentMethod.BankTransfer or PaymentMethod.Card or PaymentMethod.PosTerminal or PaymentMethod.MobileMoney;

    private static Result<CreatedSaleResult> ToResult(Sale sale) =>
        Result<CreatedSaleResult>.Ok(ToCreatedResult(sale));

    private static CreatedSaleResult ToCreatedResult(Sale sale) =>
        new(
            sale.Id,
            $"GR-{sale.Id:N}"[..15].ToUpperInvariant(),
            sale.TotalKobo,
            sale.TenderedKobo,
            sale.ChangeDueKobo,
            sale.BalanceDueKobo);
}
