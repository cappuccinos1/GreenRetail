using Microsoft.Extensions.DependencyInjection;

namespace GreenRetail.Procurement;

public static class ProcurementModule
{
    public static IServiceCollection AddProcurementFeature(this IServiceCollection services)
    {
        services.AddTransient<ICreateSupplierUseCase, CreateSupplierUseCase>();
        services.AddTransient<ICreatePurchaseOrderUseCase, CreatePurchaseOrderUseCase>();
        services.AddTransient<IPostGrnUseCase, PostGrnUseCase>();
        services.AddTransient<IStartReceivingUseCase, StartReceivingUseCase>();
        services.AddTransient<ICompleteQcUseCase, CompleteQcUseCase>();
        services.AddTransient<IConfirmNoPoReceivingUseCase, ConfirmNoPoReceivingUseCase>();
        services.AddTransient<IPostReceivingGrnUseCase, PostReceivingGrnUseCase>();

        services.AddTransient<IGetSuppliersQuery, GetSuppliersQuery>();
        services.AddTransient<IGetActivePurchaseOrdersQuery, GetActivePurchaseOrdersQuery>();

        services.AddTransient<ProcurementViewModel>();
        services.AddTransient<ProcurementPage>();

        return services;
    }
}