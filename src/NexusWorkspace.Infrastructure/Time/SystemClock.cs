using NexusWorkspace.Application.Abstractions;

namespace NexusWorkspace.Infrastructure.Time;

/// <inheritdoc cref="IClock" />
public sealed class SystemClock : IClock
{
    public DateTime UtcNow => DateTime.UtcNow;
}
