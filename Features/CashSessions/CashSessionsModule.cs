using Microsoft.Extensions.DependencyInjection;

namespace GreenRetail.Features.CashSessions;

public static class CashSessionsModule
{
    public static IServiceCollection AddCashSessionsFeature(this IServiceCollection services)
    {
        services.AddTransient<IOpenCashSessionUseCase, OpenCashSessionUseCase>();
        services.AddTransient<ICloseCashSessionUseCase, CloseCashSessionUseCase>();
        services.AddTransient<IGetOpenCashSessionQuery, GetOpenCashSessionQuery>();

        services.AddTransient<CashSessionViewModel>();
        services.AddTransient<CashSessionPage>();

        return services;
    }
}