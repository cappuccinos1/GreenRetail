namespace GreenRetail.Core.Abstractions;

public interface IClock
{
    DateTime UtcNow { get; }
}