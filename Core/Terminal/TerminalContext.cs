using Microsoft.EntityFrameworkCore;
using GreenRetail.Data;

namespace GreenRetail.Core.Terminal;

public sealed class TerminalContext : ITerminalContext
{
    public Guid TerminalId { get; private set; }
    public string TerminalName { get; private set; } = "Unknown";

    public TerminalContext(IDbContextFactory<PosDbContext> dbContextFactory)
    {
        using var db = dbContextFactory.CreateDbContext();
        var terminal = db.Terminals.FirstOrDefault();
        
        if (terminal != null)
        {
            TerminalId = terminal.Id;
            TerminalName = terminal.Name;
        }
        else
        {
            TerminalId = Guid.NewGuid();
            TerminalName = "FALLBACK-TERMINAL";
        }
    }
}