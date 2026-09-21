namespace GreenRetail.Core.ValueObjects;

public readonly record struct Money(long Kobo) : IComparable<Money>
{
    public static Money Zero => new(0);

    public static Money FromNaira(decimal naira)
    {
        return new Money((long)Math.Round(naira * 100m, MidpointRounding.AwayFromZero));
    }

    public decimal ToNaira()
    {
        return Kobo / 100m;
    }

    public static Money operator +(Money left, Money right)
        => new(left.Kobo + right.Kobo);

    public static Money operator -(Money left, Money right)
        => new(left.Kobo - right.Kobo);

    public static Money operator *(Money left, decimal quantity)
    {
        return new Money((long)Math.Round(left.Kobo * quantity, MidpointRounding.AwayFromZero));
    }

    public static bool operator >(Money left, Money right)
        => left.Kobo > right.Kobo;

    public static bool operator <(Money left, Money right)
        => left.Kobo < right.Kobo;

    public static bool operator >=(Money left, Money right)
        => left.Kobo >= right.Kobo;

    public static bool operator <=(Money left, Money right)
        => left.Kobo <= right.Kobo;

    public int CompareTo(Money other)
        => Kobo.CompareTo(other.Kobo);

    public override string ToString()
        => $"₦{ToNaira():N0}";
}