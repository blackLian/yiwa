using Companion.Desktop.Win32;

namespace Companion.Desktop;

public sealed class OverlayWindowController
{
    private readonly WindowStyleService _windowStyleService;
    private readonly ClickThroughService _clickThroughService;

    public OverlayWindowController(
        WindowStyleService windowStyleService,
        ClickThroughService clickThroughService)
    {
        _windowStyleService = windowStyleService;
        _clickThroughService = clickThroughService;
    }

    public bool IsTopMost { get; private set; } = true;
    public bool IsClickThrough => _clickThroughService.IsClickThrough;
    public double LastClickThroughSwitchDurationMs => _clickThroughService.LastSwitchDurationMs;

    public void Initialize()
    {
        _windowStyleService.EnsureTransparentTopMostWindow(IsTopMost);
    }

    public void SetTopMost(bool enabled)
    {
        IsTopMost = enabled;
        _windowStyleService.EnsureTransparentTopMostWindow(IsTopMost);
    }

    public void ToggleClickThrough()
    {
        _clickThroughService.SetClickThrough(!IsClickThrough);
    }

    public void ForceInteractiveMode()
    {
        _clickThroughService.SetClickThrough(false);
    }
}
