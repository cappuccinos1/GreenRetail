namespace GreenRetail.Core.Terminal;

public interface ITerminalContext
{
    Guid TerminalId { get; }
    Guid? BranchId { get; }
    string TerminalName { get; }
}
