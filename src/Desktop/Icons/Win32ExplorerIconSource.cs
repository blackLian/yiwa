using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using RuntimeInformation = System.Runtime.InteropServices.RuntimeInformation;

namespace Companion.Desktop.Icons;

/// <summary>
/// Windows icon source for P3.
///
/// Strategy:
/// 1) Discover Explorer desktop ListView handle and item count.
/// 2) Prefer native ListView item text + coordinates.
/// 3) Fallback to deterministic work-area grid when native read is unavailable.
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
            if (nativeCount > 0 && TryReadNativeIcons(listViewHandle, nativeCount, out var nativeIcons) && nativeIcons.Count > 0)
            {
                icons = nativeIcons;
                _consecutiveFailures = 0;
                _lastError = "native-position-and-text-mapping-active";
                return true;
            }

            var desktopNames = ReadDesktopNames();
            if (desktopNames.Count == 0)
            {
                icons = Array.Empty<DesktopIconInfo>();
                _consecutiveFailures = 0;
                _lastError = string.Empty;
                return true;
            }

            var workArea = GetDesktopWorkAreaOrDefault();
            icons = MapToRuntimeGrid(desktopNames, workArea);
            _consecutiveFailures = 0;
            _lastError = nativeCount > 0
                ? $"native-read-unavailable:fallback-grid;native={nativeCount},mapped={desktopNames.Count}"
                : "native-read-unavailable:fallback-grid";
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

    private static IReadOnlyList<string> ReadDesktopNames()
    {
        var desktopDir = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        if (string.IsNullOrWhiteSpace(desktopDir) || !Directory.Exists(desktopDir))
        {
            return Array.Empty<string>();
        }

        return Directory
            .GetFileSystemEntries(desktopDir)
            .Select(Path.GetFileName)
            .Where(static name => !string.IsNullOrWhiteSpace(name) && !name.Equals("desktop.ini", StringComparison.OrdinalIgnoreCase))
            .OrderBy(static name => name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static bool TryReadNativeIcons(IntPtr listViewHandle, int nativeCount, out IReadOnlyList<DesktopIconInfo> icons)
    {
        icons = Array.Empty<DesktopIconInfo>();

        GetWindowThreadProcessId(listViewHandle, out var processId);
        if (processId == 0)
        {
            return false;
        }

        var process = OpenProcess(PROCESS_QUERY_INFORMATION | PROCESS_VM_OPERATION | PROCESS_VM_READ | PROCESS_VM_WRITE, false, processId);
        if (process == IntPtr.Zero)
        {
            return false;
        }

        try
        {
            var pointSize = Marshal.SizeOf<NativePoint>();
            var lvItemSize = Marshal.SizeOf<NativeLvItem>();
            var pointPtr = VirtualAllocEx(process, IntPtr.Zero, (uint)pointSize, MEM_COMMIT | MEM_RESERVE, PAGE_READWRITE);
            var textPtr = VirtualAllocEx(process, IntPtr.Zero, TextBufferBytes, MEM_COMMIT | MEM_RESERVE, PAGE_READWRITE);
            var itemPtr = VirtualAllocEx(process, IntPtr.Zero, (uint)lvItemSize, MEM_COMMIT | MEM_RESERVE, PAGE_READWRITE);

            if (pointPtr == IntPtr.Zero || textPtr == IntPtr.Zero || itemPtr == IntPtr.Zero)
            {
                return false;
            }

            try
            {
                var result = new List<DesktopIconInfo>(nativeCount);
                for (var i = 0; i < nativeCount; i++)
                {
                    if (!TryReadNativePoint(listViewHandle, process, pointPtr, i, out var point))
                    {
                        continue;
                    }

                    var name = TryReadNativeText(listViewHandle, process, itemPtr, textPtr, i);
                    if (string.IsNullOrWhiteSpace(name))
                    {
                        name = $"desktop-item-{i}";
                    }

                    const float iconWidth = 92f;
                    const float iconHeight = 92f;
                    result.Add(new DesktopIconInfo(name, point.X, point.Y, iconWidth, iconHeight));
                }

                if (result.Count == 0)
                {
                    return false;
                }

                icons = result;
                return true;
            }
            finally
            {
                _ = VirtualFreeEx(process, pointPtr, 0, MEM_RELEASE);
                _ = VirtualFreeEx(process, textPtr, 0, MEM_RELEASE);
                _ = VirtualFreeEx(process, itemPtr, 0, MEM_RELEASE);
            }
        }
        finally
        {
            _ = CloseHandle(process);
        }
    }

    private static bool TryReadNativePoint(IntPtr listViewHandle, IntPtr process, IntPtr remotePoint, int index, out NativePoint point)
    {
        point = default;

        var msgResult = SendMessage(listViewHandle, LVM_GETITEMPOSITION, (IntPtr)index, remotePoint);
        if (msgResult == IntPtr.Zero)
        {
            return false;
        }

        var pointBuffer = new byte[Marshal.SizeOf<NativePoint>()];
        if (!ReadProcessMemory(process, remotePoint, pointBuffer, (nuint)pointBuffer.Length, out _))
        {
            return false;
        }

        point = ByteArrayToStructure<NativePoint>(pointBuffer);
        return ClientToScreen(listViewHandle, ref point);
    }

    private static string TryReadNativeText(IntPtr listViewHandle, IntPtr process, IntPtr remoteLvItem, IntPtr remoteText, int index)
    {
        var local = new NativeLvItem
        {
            mask = LVIF_TEXT,
            iItem = index,
            iSubItem = 0,
            cchTextMax = MaxTextChars,
            pszText = remoteText,
        };

        var localBytes = StructureToByteArray(local);
        if (!WriteProcessMemory(process, remoteLvItem, localBytes, (nuint)localBytes.Length, out _))
        {
            return string.Empty;
        }

        _ = SendMessage(listViewHandle, LVM_GETITEMTEXTW, (IntPtr)index, remoteLvItem);

        var textBytes = new byte[TextBufferBytes];
        if (!ReadProcessMemory(process, remoteText, textBytes, (nuint)textBytes.Length, out _))
        {
            return string.Empty;
        }

        var text = Encoding.Unicode.GetString(textBytes);
        var nullIdx = text.IndexOf('\0');
        if (nullIdx >= 0)
        {
            text = text[..nullIdx];
        }

        return text.Trim();
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

    private static byte[] StructureToByteArray<T>(T value) where T : struct
    {
        var size = Marshal.SizeOf<T>();
        var bytes = new byte[size];
        var handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
        try
        {
            Marshal.StructureToPtr(value, handle.AddrOfPinnedObject(), false);
            return bytes;
        }
        finally
        {
            handle.Free();
        }
    }

    private static T ByteArrayToStructure<T>(byte[] bytes) where T : struct
    {
        var handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
        try
        {
            return Marshal.PtrToStructure<T>(handle.AddrOfPinnedObject());
        }
        finally
        {
            handle.Free();
        }
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
    private const int LVM_GETITEMPOSITION = LVM_FIRST + 16;
    private const int LVM_GETITEMTEXTW = LVM_FIRST + 115;

    private const int LVIF_TEXT = 0x0001;
    private const int MaxTextChars = 260;
    private const uint TextBufferBytes = MaxTextChars * 2u;

    private const uint SPI_GETWORKAREA = 0x0030;

    private const uint MEM_COMMIT = 0x1000;
    private const uint MEM_RESERVE = 0x2000;
    private const uint MEM_RELEASE = 0x8000;
    private const uint PAGE_READWRITE = 0x0004;

    private const uint PROCESS_VM_OPERATION = 0x0008;
    private const uint PROCESS_VM_READ = 0x0010;
    private const uint PROCESS_VM_WRITE = 0x0020;
    private const uint PROCESS_QUERY_INFORMATION = 0x0400;

    [StructLayout(LayoutKind.Sequential)]
    private struct NativePoint
    {
        public int X;
        public int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeLvItem
    {
        public uint mask;
        public int iItem;
        public int iSubItem;
        public uint state;
        public uint stateMask;
        public IntPtr pszText;
        public int cchTextMax;
        public int iImage;
        public IntPtr lParam;
        public int iIndent;
        public int iGroupId;
        public uint cColumns;
        public IntPtr puColumns;
        public IntPtr piColFmt;
        public int iGroup;
    }

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

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool ClientToScreen(IntPtr hWnd, ref NativePoint lpPoint);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr OpenProcess(uint processAccess, bool inheritHandle, uint processId);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr VirtualAllocEx(IntPtr process, IntPtr address, uint size, uint allocationType, uint protect);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool VirtualFreeEx(IntPtr process, IntPtr address, uint size, uint freeType);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool ReadProcessMemory(IntPtr process, IntPtr baseAddress, [Out] byte[] buffer, nuint size, out nuint bytesRead);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool WriteProcessMemory(IntPtr process, IntPtr baseAddress, byte[] buffer, nuint size, out nuint bytesWritten);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool CloseHandle(IntPtr handle);
}
