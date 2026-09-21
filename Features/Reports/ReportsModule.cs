using Microsoft.Extensions.DependencyInjection;

namespace GreenRetail.Features.Reports;

public static class ReportsModule
{
    public static IServiceCollection AddReportsFeature(this IServiceCollection services)
    {
        services.AddTransient<IGetDailyReportQuery, GetDailyReportQuery>();

        services.AddTransient<ReportsViewModel>();
        services.AddTransient<ReportsPage>();

        return services;
    }
}