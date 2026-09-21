using GreenRetail.Data.Entities;

namespace GreenRetail.Features.Checkout;

public sealed record CreateSaleItemCommand(Guid ProductId, decimal Quantity, long UnitPriceKobo);
public sealed record CreateSalePaymentCommand(PaymentMethod Method, long AmountKobo, string? Reference);

public sealed record CreateSaleCommand(
    string IdempotencyKey,
    Guid? CashierId,
    string CashierName,
    IReadOnlyList<CreateSaleItemCommand> Items,
    IReadOnlyList<CreateSalePaymentCommand> Payments);