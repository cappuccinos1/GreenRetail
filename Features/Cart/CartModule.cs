using Microsoft.Extensions.DependencyInjection;

namespace GreenRetail.Features.Cart;

public static class CartModule
{
    public static IServiceCollection AddCartFeature(this IServiceCollection services)
    {
        services.AddSingleton<ICartService, CartService>();
        return services;
    }
}