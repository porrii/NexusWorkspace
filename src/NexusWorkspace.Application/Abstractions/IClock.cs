namespace NexusWorkspace.Application.Abstractions;

/// <summary>
/// The system clock, always in UTC. Injected everywhere instead of
/// <see cref="DateTime.UtcNow"/> so time-dependent logic is testable and the
/// history stays correct across time-zone and DST changes.
/// </summary>
public interface IClock
{
    DateTime UtcNow { get; }

    DateOnly Today => DateOnly.FromDateTime(UtcNow);
}
