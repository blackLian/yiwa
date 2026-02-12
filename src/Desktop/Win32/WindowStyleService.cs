using Companion.Desktop.Diagnostics;

namespace Companion.Desktop.Win32;

public sealed class WindowStyleService
{
    private readonly IOverlayWindowHost _host;
    private readonly IntegrationDiagnostics _diagnostics;

    public WindowStyleService(IOverlayWindowHost host, IntegrationDiagnostics diagnostics)
    {
        _host = host;
        _diagnostics = diagnostics;
    }

    public void EnsureTransparentTopMostWindow(bool topMost)
    {
        _host.EnsureCreated();
        _host.SetTransparentBackground(true);
        _host.SetTopMost(topMost);

        _diagnostics.Info(
            "WindowStyle",
            $"window={_host.WindowHandle}, transparent={_host.IsTransparentBackground}, topMost={_host.IsTopMost}");
    }
}
