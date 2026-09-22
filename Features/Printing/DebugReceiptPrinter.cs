using System.Diagnostics;
using GreenRetail.Features.Checkout;

namespace GreenRetail.Features.Printing;

public sealed class DebugReceiptPrinter : IReceiptPrinter
{
    public Task PrintAsync(CreatedSaleResult sale, CancellationToken cancellationToken = default)
    {
        Debug.WriteLine("========== RECEIPT ==========");
        Debug.WriteLine($"Sale ID: {sale.SaleId}");
        Debug.WriteLine($"Total: ₦{sale.TotalKobo / 100m:N2}");
        Debug.WriteLine($"Tendered: ₦{sale.TenderedKobo / 100m:N2}");
        Debug.WriteLine($"Change: ₦{sale.ChangeDueKobo / 100m:N2}");
        Debug.WriteLine($"Balance: ₦{sale.BalanceDueKobo / 100m:N2}");
        Debug.WriteLine("=============================");
        return Task.CompletedTask;
    }
}
