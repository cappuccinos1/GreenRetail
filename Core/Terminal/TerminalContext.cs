using Microsoft.EntityFrameworkCore;
using GreenRetail.Data;

namespace GreenRetail.Core.Terminal;

/// <summary>
/// Identifies the physical POS terminal for the current Windows installation.
/// A fabricated terminal ID is never acceptable because it would allow sales
/// and cash sessions to be written against a non-existent register.
/// </summary>
public sealed class TerminalContext : ITerminalContext
{
    public Guid TerminalId { get; }
    public Guid? BranchId { get; }
    public string TerminalName { get; }

    public TerminalContext(IDbContextFactory<PosDbContext> dbContextFactory)
    {
        using var db = dbContextFactory.CreateDbContext();
        var terminal = db.Terminals
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.CreatedUtc)
            .FirstOrDefault();

        if (terminal is null)
            throw new InvalidOperationException(
                "No active POS terminal is configured. Complete terminal setup before opening the register.");

        TerminalId = terminal.Id;
        BranchId = terminal.BranchId;
        TerminalName = terminal.Name;
    }
}
