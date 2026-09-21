using GreenRetail.Core.Abstractions;

namespace GreenRetail.Core.Time;

public sealed class SystemClock : IClock
{
    public DateTime UtcNow => DateTime.UtcNow;
}