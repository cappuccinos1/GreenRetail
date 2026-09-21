namespace GreenRetail.Core.Terminal;

public interface ITerminalContext
{
    Guid TerminalId { get; }
    string TerminalName { get; }
}