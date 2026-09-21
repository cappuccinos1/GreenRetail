using System.Diagnostics;
using GreenRetail.Features.Checkout;
using GreenRetail.Core.ValueObjects;

namespace GreenRetail.Features.Printing;

public sealed class DebugReceiptPrinter : IReceiptPrinter
{
    public Task PrintAsync(CompletedSale sale, CancellationToken cancellationToken = default)
    {
        Debug.WriteLine("========== RECEIPT ==========");
        Debug.WriteLine($"Sale ID: {sale.SaleId}");
        Debug.WriteLine($"Total: {sale.Total}");
        Debug.WriteLine($"Tendered: {sale.Tendered}");
        Debug.WriteLine($"Change: {sale.ChangeDue}");
        Debug.WriteLine($"Balance Due: {sale.BalanceDue}");
        Debug.WriteLine("=============================");

        return Task.CompletedTask;
    }
}