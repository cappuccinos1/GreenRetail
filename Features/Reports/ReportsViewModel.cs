using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GreenRetail.Core.Abstractions;

namespace GreenRetail.Features.Reports;

public partial class ReportsViewModel : ObservableObject
{
    private readonly IGetDailyReportQuery _getDailyReport;

    [ObservableProperty]
    private string reportText = "No report loaded.";

    [ObservableProperty]
    private bool isBusy;

    public ReportsViewModel(IGetDailyReportQuery getDailyReport)
    {
        _getDailyReport = getDailyReport;
    }

    public async Task InitializeAsync()
    {
        await RefreshAsync();
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        try
        {
            IsBusy = true;

            var result = await _getDailyReport.ExecuteAsync(new EmptyRequest());

            if (!result.IsSuccess)
            {
                ReportText = result.Error ?? "Unable to load report.";
                return;
            }

            var report = result.Value;

            var builder = new StringBuilder();

            builder.AppendLine($"Generated UTC: {report.GeneratedUtc:yyyy-MM-dd HH:mm}");
            builder.AppendLine($"Sales Count: {report.SalesCount}");
            builder.AppendLine($"Total Sales: {report.TotalSales}");
            builder.AppendLine($"Cash: {report.Cash}");
            builder.AppendLine($"Bank Transfer: {report.BankTransfer}");
            builder.AppendLine($"Card / POS: {report.Card}");
            builder.AppendLine($"Mobile Money: {report.MobileMoney}");
            builder.AppendLine($"Estimated Profit: {report.EstimatedProfit}");
            builder.AppendLine($"Low Stock Items: {report.LowStockCount}");

            ReportText = builder.ToString();
        }
        finally
        {
            IsBusy = false;
        }
    }
}