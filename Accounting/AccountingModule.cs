using Microsoft.Extensions.DependencyInjection;

namespace GreenRetail.Accounting;

public static class AccountingModule
{
    public static IServiceCollection AddAccountingFeature(this IServiceCollection services)
    {
        services.AddTransient<IPostManualJournalUseCase, PostManualJournalUseCase>();
        services.AddTransient<IGetTrialBalanceQuery, GetTrialBalanceQuery>();

        services.AddTransient<TrialBalanceViewModel>();
        services.AddTransient<TrialBalancePage>();

        return services;
    }
}