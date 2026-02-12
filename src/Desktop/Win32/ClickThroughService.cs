using System.Diagnostics;
using Companion.Desktop.Diagnostics;

namespace Companion.Desktop.Win32;

public sealed class ClickThroughService
{
    private readonly IOverlayWindowHost _host;
    private readonly IntegrationDiagnostics _diagnostics;

    public ClickThroughService(IOverlayWindowHost host, IntegrationDiagnostics diagnostics)
    {
        _host = host;
        _diagnostics = diagnostics;
    }

    public bool IsClickThrough => _host.IsClickThrough;
    public double LastSwitchDurationMs { get; private set; }

    public void SetClickThrough(bool enabled)
    {
        var watch = Stopwatch.StartNew();
        _host.SetClickThrough(enabled);
        watch.Stop();

        LastSwitchDurationMs = watch.Elapsed.TotalMilliseconds;
        _diagnostics.Info(
            "ClickThrough",
            $"window={_host.WindowHandle}, enabled={_host.IsClickThrough}, durationMs={LastSwitchDurationMs:F3}");
    }
}
