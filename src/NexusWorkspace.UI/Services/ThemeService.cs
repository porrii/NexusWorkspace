using Avalonia;
using Avalonia.Styling;
using Avalonia.Threading;
using NexusWorkspace.Application.Abstractions;
using NexusWorkspace.Application.Settings;

namespace NexusWorkspace.UI.Services;

public interface IThemeService
{
    ThemeMode Current { get; }

    void Apply(ThemeMode mode);

    Task SetAndPersistAsync(ThemeMode mode);
}

/// <summary>Maps <see cref="ThemeMode"/> to Avalonia's <see cref="ThemeVariant"/> and persists the choice.</summary>
public sealed class ThemeService(ISettingsStore settings) : IThemeService
{
    public ThemeMode Current { get; private set; } = ThemeMode.System;

    public void Apply(ThemeMode mode)
    {
        Current = mode;

        var variant = mode switch
        {
            ThemeMode.Light => ThemeVariant.Light,
            ThemeMode.Dark => ThemeVariant.Dark,
            _ => ThemeVariant.Default,
        };

        if (Avalonia.Application.Current is { } app)
        {
            Dispatcher.UIThread.Post(() => app.RequestedThemeVariant = variant);
        }
    }

    public async Task SetAndPersistAsync(ThemeMode mode)
    {
        Apply(mode);
        await settings.UpdateAsync(s => s.Theme = mode);
    }
}
