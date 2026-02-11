using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using RuntimeInformation = System.Runtime.InteropServices.RuntimeInformation;

namespace Companion.Desktop.Icons;

/// <summary>
/// Windows icon source for P3.
///
/// Strategy:
/// 1) Try discovering Explorer desktop ListView handle and item count.
/// 2) Read visible desktop entries from Desktop folder.
/// 3) Map entries into a deterministic layout for current runtime collision simulation.
///
/// NOTE: native per-icon ListView pixel coordinates are not wired yet. This source keeps
/// runtime flow available while exposing health details so progress tracking can remain honest.
/// </summary>
public sealed class Win32ExplorerIconSource : IDesktopIconSource, IDesktopIconSourceHealthProvider
{
    private DateTimeOffset _lastAttemptAt = DateTimeOffset.MinValue;
    private int _consecutiveFailures;
    private string _lastError = string.Empty;
    private bool _listViewDetected;

    public bool TryGetVisibleIcons(out IReadOnlyList<DesktopIconInfo> icons)
    {
        _lastAttemptAt = DateTimeOffset.UtcNow;
        icons = Array.Empty<DesktopIconInfo>();

        if (!RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows))
        {
            MarkFailure("not running on windows");
            return false;
        }

        try
        {
            if (!TryGetDesktopListView(out var listViewHandle))
            {
                MarkFailure("desktop listview not found");
                return false;
            }

            _listViewDetected = true;
            var nativeCount = SendMessage(listViewHandle, LVM_GETITEMCOUNT, IntPtr.Zero, IntPtr.Zero).ToInt32();

            var desktopDir = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            if (string.IsNullOrWhiteSpace(desktopDir) || !Directory.Exists(desktopDir))
            {
                MarkFailure("desktop directory unavailable");
                return false;
            }

            var entries = Directory.GetFileSystemEntries(desktopDir);
            var mapped = MapToRuntimeGrid(entries, nativeCount);

            icons = mapped;
            _consecutiveFailures = 0;
            _lastError = nativeCount >= 0
                ? "native-listview-detected:grid-mapping-active"
                : "native-listview-unavailable:grid-mapping-active";
            return true;
        }
        catch (Exception ex)
        {
            MarkFailure(ex.Message);
            icons = Array.Empty<DesktopIconInfo>();
            return false;
        }
    }

    public DesktopIconSourceHealth GetHealth()
    {
        var isReady = _consecutiveFailures == 0 && _listViewDetected;
        return new DesktopIconSourceHealth(
            IsReady: isReady,
            ConsecutiveFailures: _consecutiveFailures,
            LastAttemptAt: _lastAttemptAt,
            LastError: _lastError);
    }

    private static List<DesktopIconInfo> MapToRuntimeGrid(string[] entries, int nativeCount)
    {
        var mapped = new List<DesktopIconInfo>(entries.Length);

        const float originX = 28f;
        const float originY = 28f;
        const float iconWidth = 92f;
        const float iconHeight = 92f;
        const float rowGap = 16f;
        const int fallbackRows = 11;

        var rows = nativeCount > 0 ? Math.Clamp(nativeCount, 6, 18) : fallbackRows;

        for (var i = 0; i < entries.Length; i++)
        {
            var name = Path.GetFileName(entries[i]);
            if (string.IsNullOrWhiteSpace(name))
            {
                continue;
            }

            var col = i / rows;
            var row = i % rows;
            var x = originX + col * iconWidth;
            var y = originY + row * (iconHeight + rowGap);
            mapped.Add(new DesktopIconInfo(name, x, y, iconWidth, iconHeight));
        }

        return mapped;
    }

    private static bool TryGetDesktopListView(out IntPtr listViewHandle)
    {
        listViewHandle = IntPtr.Zero;

        var progman = FindWindow("Progman", "Program Manager");
        if (progman == IntPtr.Zero)
        {
            return false;
        }

        var shellView = FindWindowEx(progman, IntPtr.Zero, "SHELLDLL_DefView", null);
        if (shellView == IntPtr.Zero)
        {
            var worker = IntPtr.Zero;
            while (true)
            {
                worker = FindWindowEx(IntPtr.Zero, worker, "WorkerW", null);
                if (worker == IntPtr.Zero)
                {
                    break;
                }

                shellView = FindWindowEx(worker, IntPtr.Zero, "SHELLDLL_DefView", null);
                if (shellView != IntPtr.Zero)
                {
                    break;
                }
            }
        }

        if (shellView == IntPtr.Zero)
        {
            return false;
        }

        listViewHandle = FindWindowEx(shellView, IntPtr.Zero, "SysListView32", "FolderView");
        return listViewHandle != IntPtr.Zero;
    }

    private void MarkFailure(string error)
    {
        _consecutiveFailures++;
        _listViewDetected = false;
        _lastError = error;
    }

    private const int LVM_FIRST = 0x1000;
    private const int LVM_GETITEMCOUNT = LVM_FIRST + 4;

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr FindWindow(string? lpClassName, string? lpWindowName);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr FindWindowEx(
        IntPtr hWndParent,
        IntPtr hWndChildAfter,
        string? lpszClass,
        string? lpszWindow);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);
}
