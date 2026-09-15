using NexusWorkspace.Application.Abstractions;
using NexusWorkspace.Application.Settings;

namespace NexusWorkspace.Tests.TestSupport;

/// <summary>In-memory <see cref="ISettingsStore"/> for tests — no disk persistence.</summary>
public sealed class InMemorySettingsStore : ISettingsStore
{
    public AppSettings Current { get; } = new();

    public Task UpdateAsync(Action<AppSettings> mutate, CancellationToken cancellationToken = default)
    {
        mutate(Current);
        Changed?.Invoke(this, EventArgs.Empty);
        return Task.CompletedTask;
    }

    public Task ReloadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public event EventHandler? Changed;
}
