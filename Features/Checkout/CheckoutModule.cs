using Microsoft.Extensions.DependencyInjection;

namespace GreenRetail.Features.Checkout;

public static class CheckoutModule
{
    public static IServiceCollection AddCheckoutFeature(this IServiceCollection services)
    {
        services.AddTransient<ICreateSaleUseCase, CreateSaleUseCase>();
        services.AddTransient<CheckoutViewModel>();
        services.AddTransient<CheckoutPage>();
        return services;
    }
}
