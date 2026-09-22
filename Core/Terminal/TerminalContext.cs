using Microsoft.EntityFrameworkCore;
using GreenRetail.Data;

namespace GreenRetail.Core.Terminal;

public sealed class TerminalContext : ITerminalContext
{
    public Guid TerminalId { get; private set; }
    public Guid? BranchId { get; private set; }
    public string TerminalName { get; private set; } = "Terminal not configured";

    public TerminalContext(IDbContextFactory<PosDbContext> dbContextFactory)
    {
        using var db = dbContextFactory.CreateDbContext();
        var terminal = db.Terminals.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.CreatedUtc).FirstOrDefault();
        if (terminal is null) return;
        TerminalId = terminal.Id;
        BranchId = terminal.BranchId;
        TerminalName = terminal.Name;
    }
}
