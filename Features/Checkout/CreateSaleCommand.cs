using GreenRetail.Data.Entities;

namespace GreenRetail.Features.Checkout;

public sealed record CreateSaleItemCommand(
    Guid ProductId,
    decimal Quantity,
    long? UnitPriceKobo = null);

/// <summary>
/// Payment amount is the amount applied to the sale, not the physical cash tendered.
/// For cash payments, CashTenderedKobo carries the amount handed to the cashier so
/// change can be calculated without overstating the cash drawer.
/// </summary>
public sealed record CreateSalePaymentCommand(
    PaymentMethod Method,
    long AmountKobo,
    string? Reference,
    string? IdempotencyKey = null);

public sealed record CreateSaleCommand(
    string IdempotencyKey,
    Guid? CashierId,
    string CashierName,
    IReadOnlyList<CreateSaleItemCommand> Items,
    IReadOnlyList<CreateSalePaymentCommand> Payments,
    long? CashTenderedKobo = null);
