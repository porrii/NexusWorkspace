using NexusWorkspace.Application.Settings;

namespace NexusWorkspace.Application.Abstractions;

/// <summary>
/// Loads and persists <see cref="AppSettings"/> to <c>settings.json</c>. A single
/// instance is shared for the process lifetime.
/// </summary>
public interface ISettingsStore
{
    AppSettings Current { get; }

    /// <summary>Mutates the settings and persists them. Raises <see cref="Changed"/>.</summary>
    Task UpdateAsync(Action<AppSettings> mutate, CancellationToken cancellationToken = default);

    /// <summary>Reloads from disk, discarding unsaved in-memory changes.</summary>
    Task ReloadAsync(CancellationToken cancellationToken = default);

    event EventHandler? Changed;
}
