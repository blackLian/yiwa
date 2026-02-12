using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using RuntimeInformation = System.Runtime.InteropServices.RuntimeInformation;

namespace Companion.Desktop.Icons;

/// <summary>
/// Windows icon source for P3.
///
/// Strategy:
/// 1) Discover Explorer desktop ListView handle and item count.
/// 2) Read visible desktop entries from Desktop folder.
/// 3) Map entries into a deterministic grid inside the desktop work area.
///
/// NOTE: native per-icon ListView pixel coordinates are not wired yet.
/// This source keeps runtime flow available while exposing health details.
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

            var entries = Directory
                .GetFileSystemEntries(desktopDir)
                .Select(Path.GetFileName)
                .Where(static name => !string.IsNullOrWhiteSpace(name) && !name.Equals("desktop.ini", StringComparison.OrdinalIgnoreCase))
                .OrderBy(static name => name, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            if (entries.Length == 0)
            {
                _consecutiveFailures = 0;
                _lastError = string.Empty;
                icons = Array.Empty<DesktopIconInfo>();
                return true;
            }

            var workArea = GetDesktopWorkAreaOrDefault();
            var mapped = MapToRuntimeGrid(entries, workArea);

            icons = mapped;
            _consecutiveFailures = 0;
            _lastError = nativeCount >= 0 && Math.Abs(nativeCount - mapped.Count) > 20
                ? $"native-count-mismatch:native={nativeCount},mapped={mapped.Count}"
                : string.Empty;
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

    private static List<DesktopIconInfo> MapToRuntimeGrid(IReadOnlyList<string> names, Win32Rect workArea)
    {
        var mapped = new List<DesktopIconInfo>(names.Count);

        const float iconWidth = 92f;
        const float iconHeight = 92f;
        const float rowGap = 16f;

        var startX = Math.Max(workArea.Left + 16f, 12f);
        var startY = Math.Max(workArea.Top + 16f, 12f);
        var usableHeight = Math.Max(workArea.Height - 24f, iconHeight + rowGap);
        var rows = Math.Max(1, (int)(usableHeight / (iconHeight + rowGap)));

        for (var i = 0; i < names.Count; i++)
        {
            var col = i / rows;
            var row = i % rows;
            var x = startX + col * iconWidth;
            var y = startY + row * (iconHeight + rowGap);
            mapped.Add(new DesktopIconInfo(names[i], x, y, iconWidth, iconHeight));
        }

        return mapped;
    }

    private static Win32Rect GetDesktopWorkAreaOrDefault()
    {
        if (SystemParametersInfo(SPI_GETWORKAREA, 0, out var rect, 0))
        {
            return rect;
        }

        return new Win32Rect(0, 0, 1920, 1080);
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
    private const uint SPI_GETWORKAREA = 0x0030;

    [StructLayout(LayoutKind.Sequential)]
    private readonly struct Win32Rect
    {
        public Win32Rect(int left, int top, int right, int bottom)
        {
            Left = left;
            Top = top;
            Right = right;
            Bottom = bottom;
        }

        public int Left { get; }
        public int Top { get; }
        public int Right { get; }
        public int Bottom { get; }
        public int Width => Right - Left;
        public int Height => Bottom - Top;
    }

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

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SystemParametersInfo(uint uiAction, uint uiParam, out Win32Rect pvParam, uint fWinIni);
}
