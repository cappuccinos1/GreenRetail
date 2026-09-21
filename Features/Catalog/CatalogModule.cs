using Microsoft.Extensions.DependencyInjection;

namespace GreenRetail.Features.Catalog;

public static class CatalogModule
{
    public static IServiceCollection AddCatalogFeature(this IServiceCollection services)
    {
        services.AddTransient<ISearchProductsQuery, SearchProductsQuery>();
        services.AddTransient<PosViewModel>();
        services.AddTransient<PosPage>();

        return services;
    }
}