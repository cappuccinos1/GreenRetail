using GreenRetail.Core.Terminal;
using GreenRetail.Data;
using GreenRetail.Data.Entities;
using GreenRetail.Shared.State;
using Microsoft.EntityFrameworkCore;

namespace GreenRetail.Features;

public partial class PointOfSaleDashboardPage : ContentPage
{
    private readonly IDbContextFactory<PosDbContext> _dbFactory;
    private readonly ICurrentUserService _currentUser;
    private readonly ITerminalContext _terminal;

    public PointOfSaleDashboardPage(
        IDbContextFactory<PosDbContext> dbFactory,
        ICurrentUserService currentUser,
        ITerminalContext terminal)
    {
        InitializeComponent();
        _dbFactory = dbFactory;
        _currentUser = currentUser;
        _terminal = terminal;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        OperatorLabel.Text = $"{_currentUser.DisplayName ?? "User"} · {_currentUser.Role ?? "Unknown role"}";
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            var branchId = _terminal.BranchId;
            var terminalId = _terminal.TerminalId;

            var session = await db.CashSessions.AsNoTracking()
                .Where(x => x.TerminalId == terminalId && (x.Status == CashSessionStatus.Open || x.Status == CashSessionStatus.Counting))
                .OrderByDescending(x => x.OpenedUtc)
                .FirstOrDefaultAsync();

            if (session is not null)
            {
                SessionBanner.BackgroundColor = "#ECFDF5";
                SessionBanner.Stroke = new SolidColorBrush(Color.FromArgb("#A7F3D0"));
                SessionTitle.Text = session.Status == CashSessionStatus.Open ? "Register open" : "Register sent for blind count";
                SessionTitle.TextColor = Color.FromArgb("#065F46");
                SessionDetail.Text = $"Cashier: {session.CashierName}. {(_currentUser.UserId == session.CashierId ? "This terminal is ready for sales." : "Follow the cash-session workflow before selling.")}";
                SessionDetail.TextColor = Color.FromArgb("#047857");
            }
            else
            {
                SessionBanner.BackgroundColor = "#FEF2F2";
                SessionTitle.Text = "Register closed";
                SessionTitle.TextColor = Color.FromArgb("#991B1B");
                SessionDetail.Text = "Open a cash session before starting a sale.";
                SessionDetail.TextColor = Color.FromArgb("#7F1D1D");
            }

            if (branchId is null || branchId == Guid.Empty)
            {
                RecentLabel.Text = "This terminal is not assigned to a branch yet.";
                return;
            }

            var start = DateTime.UtcNow.Date;
            var end = start.AddDays(1);
            var sales = db.Sales.AsNoTracking().Where(x => x.Status == SaleStatus.Completed && x.Terminal != null && x.Terminal.BranchId == branchId && x.CreatedUtc >= start && x.CreatedUtc < end);

            TransactionsLabel.Text = (await sales.CountAsync()).ToString("N0");
            ItemsLabel.Text = (await sales.SelectMany(x => x.Items).SumAsync(x => (decimal?)x.Quantity) ?? 0m).ToString("N0");
            CashLabel.Text = (await sales.SelectMany(x => x.Payments).CountAsync(x => x.Status == PaymentStatus.Captured && x.Method == PaymentMethod.Cash)).ToString("N0");
            DigitalLabel.Text = (await sales.SelectMany(x => x.Payments).CountAsync(x => x.Status == PaymentStatus.Captured && x.Method != PaymentMethod.Cash)).ToString("N0");

            var recent = await sales.OrderByDescending(x => x.CreatedUtc).Take(5).Select(x => new { x.CreatedUtc, x.TotalKobo, x.CashierName }).ToListAsync();
            RecentLabel.Text = recent.Count == 0
                ? "No posted sales yet today."
                : string.Join("\n", recent.Select(x => $"{x.CreatedUtc.ToLocalTime():HH:mm} · {x.CashierName} · ₦{x.TotalKobo / 100m:N2}"));
        }
        catch (Exception ex)
        {
            RecentLabel.Text = $"POS dashboard could not load: {ex.Message}";
            App.LogCrash(ex);
        }
    }

    private static Task Go(string route) => Shell.Current?.GoToAsync(route) ?? Task.CompletedTask;
    private async void OnCashierClicked(object sender, EventArgs e) => await Go("pos-cashier");
    private async void OnCashSessionClicked(object sender, EventArgs e) => await Go("cash-sessions");
    private async void OnSecurityClicked(object sender, EventArgs e) => await Go("security");
}
