using Microsoft.Extensions.Logging;
using GreenRetail.Accounting;
using GreenRetail.BackOffice;
using GreenRetail.Core;
using GreenRetail.Core.Terminal;
using GreenRetail.Data;
using GreenRetail.Features.Auth;
using GreenRetail.Features.Cart;
using GreenRetail.Features.CashSessions;
using GreenRetail.Features.Catalog;
using GreenRetail.Features.Checkout;
using GreenRetail.Features.Dashboard;
using GreenRetail.Features.Hubs;
using GreenRetail.Features.Printing;
using GreenRetail.Features.Register;
using GreenRetail.Features.Reports;
using GreenRetail.Features.Sync;
using GreenRetail.InventoryOps;
using GreenRetail.Procurement;
using GreenRetail.Rbac;
using GreenRetail.Refunds;
using GreenRetail.SecurityOps;

namespace GreenRetail;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder.UseMauiApp<App>();
        builder.Logging.AddDebug();

        builder.Services.AddCore();
        builder.Services.AddData();
        
        builder.Services.AddSingleton<ITerminalContext, TerminalContext>();

        builder.Services.AddAuthFeature();
        builder.Services.AddCartFeature();
        builder.Services.AddCatalogFeature();
        
        builder.Services.AddCheckoutFeature();
        builder.Services.AddCashSessionsFeature();
        builder.Services.AddReportsFeature();
        builder.Services.AddSyncFeature();
        builder.Services.AddPrintingFeature();
        builder.Services.AddAccountingFeature();
        builder.Services.AddRbacFeature();
        builder.Services.AddInventoryOpsFeature();
        builder.Services.AddRefundsFeature();
        builder.Services.AddSecurityOpsFeature();
        builder.Services.AddBackOfficeFeature();
        builder.Services.AddProcurementFeature();

        builder.Services.AddTransient<SellHubPage>();
        builder.Services.AddTransient<InventoryHubPage>();
        builder.Services.AddTransient<PurchasingHubPage>();
        builder.Services.AddTransient<QcHubPage>();
        builder.Services.AddTransient<AccountsHubPage>();
        builder.Services.AddTransient<AuditHubPage>();
        builder.Services.AddTransient<SettingsHubPage>();
        builder.Services.AddTransient<DashboardViewModel>();
        builder.Services.AddTransient<DashboardPage>();
        builder.Services.AddTransient<RegisterViewModel>();
        builder.Services.AddTransient<RegisterPage>();

        return builder.Build();
    }
}