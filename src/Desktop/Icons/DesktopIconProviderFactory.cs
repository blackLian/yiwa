using System;
using Companion.Desktop.Diagnostics;

namespace Companion.Desktop.Icons;

public static class DesktopIconProviderFactory
{
    public static DesktopIconProvider CreateDefault(IntegrationDiagnostics diagnostics)
    {
        IDesktopIconSource source = new Win32ExplorerIconSource();
        source = new RetryingDesktopIconSource(source, TimeSpan.FromMilliseconds(500));
        return new DesktopIconProvider(diagnostics, source);
    }
}
