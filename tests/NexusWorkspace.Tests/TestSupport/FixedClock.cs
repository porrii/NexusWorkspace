using NexusWorkspace.Application.Abstractions;

namespace NexusWorkspace.Tests.TestSupport;

/// <summary>Deterministic clock for tests. Advance time with <see cref="Advance"/>.</summary>
public sealed class FixedClock(DateTime utcNow) : IClock
{
    public DateTime UtcNow { get; private set; } = utcNow;

    public void Advance(TimeSpan by) => UtcNow = UtcNow.Add(by);
}
