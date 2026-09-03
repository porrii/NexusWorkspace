using System.Diagnostics;
using Microsoft.Extensions.Logging;
using NexusWorkspace.Application.Abstractions;

namespace NexusWorkspace.Desktop.Platform;

/// <summary>Opens folders in Explorer and links in the default browser.</summary>
public sealed class WindowsPlatformLauncher(ILogger<WindowsPlatformLauncher> logger) : IPlatformLauncher
{
    public void OpenFolder(string path)
    {
        try
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "No se pudo abrir la carpeta {Path}.", path);
        }
    }

    public void OpenUrl(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "No se pudo abrir el enlace {Url}.", url);
        }
    }
}
