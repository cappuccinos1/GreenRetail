using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using GreenRetail.Core.ValueObjects;

namespace GreenRetail.Features.Cart;

public sealed class CartService : ObservableObject, ICartService
{
    private Money _subtotal;

    public ObservableCollection<CartLine> Lines { get; } = new();

    public Money Subtotal
    {
        get => _subtotal;
        private set => SetProperty(ref _subtotal, value);
    }


    public void Add(Guid productId, string name, Money unitPrice, bool isWeighed, decimal quantity = 1m)
    {
        Add(productId, name, unitPrice.ToNaira(), isWeighed, quantity);
    }

    public void Add(Guid productId, string name, decimal unitPriceNaira, bool isWeighed, decimal quantity = 1m)
    {
        if (quantity <= 0m)
            quantity = 1m;

        var existing = Lines.FirstOrDefault(x => x.ProductId == productId);

        if (existing is not null)
        {
            Lines.Remove(existing);

            Lines.Add(existing with
            {
                Quantity = existing.Quantity + quantity
            });
        }
        else
        {
            Lines.Add(new CartLine(
                productId,
                name,
                unitPriceNaira,
                quantity,
                isWeighed));
        }

        Recalculate();
        OnPropertyChanged(nameof(Lines));
    }

    public void SetQuantity(Guid productId, decimal quantity)
    {
        var existing = Lines.FirstOrDefault(x => x.ProductId == productId);
        if (existing is null)
            return;

        if (quantity <= 0m)
        {
            Remove(productId);
            return;
        }

        Lines.Remove(existing);
        Lines.Add(existing with { Quantity = quantity });
        Recalculate();
        OnPropertyChanged(nameof(Lines));
    }

    public void Remove(Guid productId)
    {
        var existing = Lines.FirstOrDefault(x => x.ProductId == productId);

        if (existing is null)
            return;

        Lines.Remove(existing);

        Recalculate();
        OnPropertyChanged(nameof(Lines));
    }

    public void Clear()
    {
        Lines.Clear();

        Recalculate();
        OnPropertyChanged(nameof(Lines));
    }

    private void Recalculate()
    {
        Subtotal = Lines.Aggregate(Money.Zero, (sum, line) => sum + line.LineTotal);
    }
}