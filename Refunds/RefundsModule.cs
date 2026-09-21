using Microsoft.Extensions.DependencyInjection;

namespace GreenRetail.Refunds;

public static class RefundsModule
{
    public static IServiceCollection AddRefundsFeature(this IServiceCollection services)
    {
        services.AddTransient<IIssueStoreCreditVoucherUseCase, IssueStoreCreditVoucherUseCase>();
        services.AddTransient<IRedeemStoreCreditVoucherUseCase, RedeemStoreCreditVoucherUseCase>();
        services.AddTransient<IInitiateRefundRequestUseCase, InitiateRefundRequestUseCase>();
        services.AddTransient<IApproveRefundRequestUseCase, ApproveRefundRequestUseCase>();

        services.AddTransient<IGetPendingRefundsQuery, GetPendingRefundsQuery>();
        services.AddTransient<IGetActiveVouchersQuery, GetActiveVouchersQuery>();

        services.AddTransient<RefundsViewModel>();
        services.AddTransient<RefundsPage>();

        return services;
    }
}