using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using Microsoft.Extensions.Logging;
using NexusWorkspace.Application.Abstractions;
using NexusWorkspace.UI.Services;

namespace NexusWorkspace.Desktop.Platform;

/// <summary>
/// Registers Ctrl+Shift+Space as an OS-wide hotkey that opens the quick-capture
/// window. Best-effort: any failure is logged and the app carries on without it.
/// </summary>
public sealed class WindowsGlobalHotkeyService(
    IQuickCaptureLauncher launcher,
    ILogger<WindowsGlobalHotkeyService> logger) : IGlobalHotkeyService
{
    private const int WmHotkey = 0x0312;
    private const int HotkeyId = 0xB001;
    private const uint ModAlt = 0x0001;
    private const uint ModControl = 0x0002;
    private const uint ModShift = 0x0004;
    private const uint ModNoRepeat = 0x4000;
    private const uint VkSpace = 0x20;

    private Window? _window;
    private nint _handle;
    private bool _registered;

    public void Start()
    {
        try
        {
            if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime { MainWindow: { } window })
            {
                return;
            }

            _window = window;
            _handle = window.TryGetPlatformHandle()?.Handle ?? nint.Zero;
            if (_handle == nint.Zero)
            {
                logger.LogWarning("No se pudo obtener el handle de la ventana; sin atajo global.");
                return;
            }

            Win32Properties.AddWndProcHookCallback(window, WndProcHook);

            _registered = RegisterHotKey(_handle, HotkeyId, ModControl | ModShift | ModNoRepeat, VkSpace);
            if (!_registered)
            {
                logger.LogWarning(
                    "No se pudo registrar Ctrl+Shift+Espacio (¿lo usa otra aplicación?). Error {Error}.",
                    Marshal.GetLastWin32Error());
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Fallo al inicializar el atajo global de captura rápida.");
        }
    }

    public void Stop()
    {
        try
        {
            if (_registered && _handle != nint.Zero)
            {
                UnregisterHotKey(_handle, HotkeyId);
                _registered = false;
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Fallo al liberar el atajo global.");
        }
    }

    private nint WndProcHook(nint hWnd, uint msg, nint wParam, nint lParam, ref bool handled)
    {
        if (msg == WmHotkey && wParam == HotkeyId)
        {
            handled = true;
            Dispatcher.UIThread.Post(() => launcher.Toggle());
        }

        return nint.Zero;
    }

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool RegisterHotKey(nint hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnregisterHotKey(nint hWnd, int id);
}
