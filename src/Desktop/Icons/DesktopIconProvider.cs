using System;
using System.Collections.Generic;
using Companion.Desktop.Diagnostics;

namespace Companion.Desktop.Icons;

public interface IDesktopIconProvider
{
    IReadOnlyList<DesktopIconInfo> GetVisibleIcons();
}

public interface IDesktopIconSource
{
    bool TryGetVisibleIcons(out IReadOnlyList<DesktopIconInfo> icons);
}

public sealed class DesktopIconProvider : IDesktopIconProvider
{
    private readonly IntegrationDiagnostics _diagnostics;
    private readonly IDesktopIconSource _iconSource;
    private readonly List<DesktopIconInfo> _cached = new();

    public DesktopIconProvider(IntegrationDiagnostics diagnostics, IDesktopIconSource iconSource)
    {
        _diagnostics = diagnostics;
        _iconSource = iconSource;
    }

    public bool LastRefreshSucceeded { get; private set; }
    public int CachedCount => _cached.Count;
    public int SourceFailureCount { get; private set; }
    public int ConsecutiveSourceFailures { get; private set; }

    public IReadOnlyList<DesktopIconInfo> GetVisibleIcons()
    {
        if (_iconSource.TryGetVisibleIcons(out var icons))
        {
            _cached.Clear();
            _cached.AddRange(icons);
            LastRefreshSucceeded = true;
            ConsecutiveSourceFailures = 0;
            _diagnostics.Info("DesktopIconProvider", $"icon source refreshed: count={_cached.Count}");
            return _cached;
        }

        LastRefreshSucceeded = false;
        SourceFailureCount++;
        ConsecutiveSourceFailures++;

        var healthText = string.Empty;
        if (_iconSource is IDesktopIconSourceHealthProvider healthProvider)
        {
            var health = healthProvider.GetHealth();
            healthText = $"; sourceReady={health.IsReady}; sourceFailures={health.ConsecutiveFailures}; lastError={health.LastError}";
        }

        _diagnostics.Warn(
            "DesktopIconProvider",
            $"icon source unavailable, using cached icons; cached={_cached.Count}; failures={ConsecutiveSourceFailures}{healthText}");
        return _cached;
    }

    public void SetCachedIcons(IEnumerable<DesktopIconInfo> icons)
    {
        _cached.Clear();
        _cached.AddRange(icons);
        LastRefreshSucceeded = false;
        _diagnostics.Info("DesktopIconProvider", $"cached icons injected: count={_cached.Count}");
    }
}

public sealed class EmptyDesktopIconSource : IDesktopIconSource
{
    public bool TryGetVisibleIcons(out IReadOnlyList<DesktopIconInfo> icons)
    {
        icons = Array.Empty<DesktopIconInfo>();
        return false;
    }
}
