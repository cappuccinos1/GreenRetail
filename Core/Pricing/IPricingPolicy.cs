using GreenRetail.Core.ValueObjects;

namespace GreenRetail.Core.Pricing;

public interface IPricingPolicy
{
    Money RoundTotal(Money total);
    Money CalculateChange(Money total, Money tendered);
    Money CalculateBalanceDue(Money total, Money tendered);
}