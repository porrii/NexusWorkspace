using System.Text.Json;
using System.Text.Json.Serialization;
using NexusWorkspace.Application.Abstractions;
using NexusWorkspace.Application.Settings;

namespace NexusWorkspace.Infrastructure.Settings;

/// <summary>
/// Persists <see cref="AppSettings"/> to <c>settings.json</c>. A corrupted file
/// never blocks startup — defaults are used and the file is rewritten on next save.
/// Writes are atomic (temp file + move).
/// </summary>
public sealed class JsonSettingsStore : ISettingsStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly string _path;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public JsonSettingsStore(IAppPaths paths)
    {
        _path = paths.SettingsFilePath;
        Current = Load(_path);
    }

    public AppSettings Current { get; private set; }

    public event EventHandler? Changed;

    public async Task UpdateAsync(Action<AppSettings> mutate, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(mutate);

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            mutate(Current);
            var json = JsonSerializer.Serialize(Current, SerializerOptions);
            var tempPath = _path + ".tmp";
            await File.WriteAllTextAsync(tempPath, json, cancellationToken).ConfigureAwait(false);
            File.Move(tempPath, _path, overwrite: true);
        }
        finally
        {
            _gate.Release();
        }

        Changed?.Invoke(this, EventArgs.Empty);
    }

    public async Task ReloadAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            Current = Load(_path);
        }
        finally
        {
            _gate.Release();
        }

        Changed?.Invoke(this, EventArgs.Empty);
    }

    private static AppSettings Load(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                var json = File.ReadAllText(path);
                var loaded = JsonSerializer.Deserialize<AppSettings>(json, SerializerOptions);
                if (loaded is not null)
                {
                    return loaded;
                }
            }
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
        {
            // Keep defaults; the file is rewritten on the next successful save.
        }

        return new AppSettings();
    }
}
