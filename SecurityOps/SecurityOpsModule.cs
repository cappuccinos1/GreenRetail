using Microsoft.Extensions.DependencyInjection;

namespace GreenRetail.SecurityOps;

public static class SecurityOpsModule
{
    public static IServiceCollection AddSecurityOpsFeature(this IServiceCollection services)
    {
        services.AddTransient<IVerifyReceiptUseCase, VerifyReceiptUseCase>();
        services.AddTransient<IReportSecurityIncidentUseCase, ReportSecurityIncidentUseCase>();
        services.AddTransient<IGetRecentSalesQuery, GetRecentSalesQuery>();

        services.AddTransient<SecurityViewModel>();
        services.AddTransient<SecurityPage>();

        return services;
    }
}