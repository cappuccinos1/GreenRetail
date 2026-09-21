using Microsoft.Extensions.DependencyInjection;

namespace GreenRetail.Features.Printing;

public static class PrintingModule
{
    public static IServiceCollection AddPrintingFeature(this IServiceCollection services)
    {
        services.AddSingleton<IReceiptPrinter, DebugReceiptPrinter>();
        return services;
    }
}