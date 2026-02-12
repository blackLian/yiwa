using System;

namespace Companion.Desktop.Win32;

public interface IOverlayWindowHost
{
    nint WindowHandle { get; }
    bool IsCreated { get; }
    bool IsTopMost { get; }
    bool IsTransparentBackground { get; }
    bool IsClickThrough { get; }

    void EnsureCreated();
    void SetTopMost(bool enabled);
    void SetTransparentBackground(bool enabled);
    void SetClickThrough(bool enabled);
}

public sealed class OverlayWindowHost : IOverlayWindowHost
{
    private static int _nextHandle = 100;

    public nint WindowHandle { get; private set; }
    public bool IsCreated => WindowHandle != 0;
    public bool IsTopMost { get; private set; }
    public bool IsTransparentBackground { get; private set; }
    public bool IsClickThrough { get; private set; }

    public void EnsureCreated()
    {
        // P3 阶段先提供状态化宿主：便于逻辑联调与测试。
        if (IsCreated)
        {
            return;
        }

        WindowHandle = (nint)_nextHandle++;
    }

    public void SetTopMost(bool enabled)
    {
        EnsureCreated();
        // TODO(P3): 调用 SetWindowPos(HWND_TOPMOST/HWND_NOTOPMOST)
        IsTopMost = enabled;
    }

    public void SetTransparentBackground(bool enabled)
    {
        EnsureCreated();
        // TODO(P3): 配置分层窗口透明样式
        IsTransparentBackground = enabled;
    }

    public void SetClickThrough(bool enabled)
    {
        EnsureCreated();
        // TODO(P3): 在 Windows 下切换 WS_EX_TRANSPARENT 扩展样式。
        IsClickThrough = enabled;
    }
}
