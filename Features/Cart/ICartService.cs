using System.Collections.ObjectModel;
using System.ComponentModel;
using GreenRetail.Core.ValueObjects;

namespace GreenRetail.Features.Cart;

public sealed record CartLine(
    Guid ProductId,
    string Name,
    Money UnitPrice,
    decimal Quantity,
    bool IsWeighed)
{
    public Money LineTotal => UnitPrice * Quantity;
}

public interface ICartService : INotifyPropertyChanged
{
    ObservableCollection<CartLine> Lines { get; }
    Money Subtotal { get; }

    void Add(Guid productId, string name, Money unitPrice, bool isWeighed, decimal quantity = 1m);
    void Remove(Guid productId);
    void Clear();
}