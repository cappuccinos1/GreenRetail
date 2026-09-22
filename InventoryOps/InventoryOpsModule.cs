using Microsoft.Extensions.DependencyInjection;

namespace GreenRetail.InventoryOps;

public static class InventoryOpsModule
{
    public static IServiceCollection AddInventoryOpsFeature(this IServiceCollection services)
    {
        services.AddTransient<IRequestStockAdjustmentUseCase, RequestStockAdjustmentUseCase>();
        services.AddTransient<IApproveStockAdjustmentUseCase, ApproveStockAdjustmentUseCase>();
        services.AddTransient<IRequestStockOverrideUseCase, RequestStockOverrideUseCase>();
        services.AddTransient<IResolveStockOverrideUseCase, ResolveStockOverrideUseCase>();

        services.AddTransient<IGetPendingStockAdjustmentsQuery, GetPendingStockAdjustmentsQuery>();
        services.AddTransient<IGetPendingStockOverridesQuery, GetPendingStockOverridesQuery>();
        services.AddTransient<IGetReadyReceivingQuery, GetReadyReceivingQuery>();

        services.AddTransient<InventoryOpsViewModel>();
        services.AddTransient<InventoryOpsPage>();

        return services;
    }
}