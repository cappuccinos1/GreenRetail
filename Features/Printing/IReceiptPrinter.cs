using GreenRetail.Features.Checkout;

namespace GreenRetail.Features.Printing;

public interface IReceiptPrinter
{
    Task PrintAsync(CreatedSaleResult sale, CancellationToken cancellationToken = default);
}
