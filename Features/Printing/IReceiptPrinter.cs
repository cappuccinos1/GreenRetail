using GreenRetail.Features.Checkout;

namespace GreenRetail.Features.Printing;

public interface IReceiptPrinter
{
    Task PrintAsync(CompletedSale sale, CancellationToken cancellationToken = default);
}