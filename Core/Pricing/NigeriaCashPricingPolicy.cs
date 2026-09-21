using GreenRetail.Core.ValueObjects;

namespace GreenRetail.Core.Pricing;

public sealed class NigeriaCashPricingPolicy : IPricingPolicy
{
    private readonly long _roundingStepKobo = 100; // ₦1

    public Money RoundTotal(Money total)
    {
        if (_roundingStepKobo <= 0)
            return total;

        if (total.Kobo < 0)
        {
            var roundedNegative = (long)Math.Round(
                total.Kobo / (double)_roundingStepKobo,
                MidpointRounding.AwayFromZero) * _roundingStepKobo;

            return new Money(roundedNegative);
        }

        var remainder = total.Kobo % _roundingStepKobo;

        if (remainder == 0)
            return total;

        var roundedDown = total.Kobo - remainder;
        var roundedUp = roundedDown + _roundingStepKobo;

        return remainder * 2 >= _roundingStepKobo
            ? new Money(roundedUp)
            : new Money(roundedDown);
    }

    public Money CalculateChange(Money total, Money tendered)
    {
        if (tendered.Kobo <= total.Kobo)
            return Money.Zero;

        return tendered - total;
    }

    public Money CalculateBalanceDue(Money total, Money tendered)
    {
        if (tendered.Kobo >= total.Kobo)
            return Money.Zero;

        return total - tendered;
    }
}