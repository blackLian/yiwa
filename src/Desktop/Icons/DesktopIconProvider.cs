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
    public string LastSourceError { get; private set; } = string.Empty;

    public IReadOnlyList<DesktopIconInfo> GetVisibleIcons()
    {
        if (_iconSource.TryGetVisibleIcons(out var icons))
        {
            _cached.Clear();
            _cached.AddRange(icons);
            LastRefreshSucceeded = true;
            ConsecutiveSourceFailures = 0;
            if (_iconSource is IDesktopIconSourceHealthProvider sourceHealth)
            {
                LastSourceError = sourceHealth.GetHealth().LastError;
            }
            else
            {
                LastSourceError = string.Empty;
            }

            _diagnostics.Info("DesktopIconProvider", $"icon source refreshed: count={_cached.Count}; lastError={LastSourceError}");
            return _cached;
        }

        LastRefreshSucceeded = false;
        SourceFailureCount++;
        ConsecutiveSourceFailures++;

        var healthText = string.Empty;
        if (_iconSource is IDesktopIconSourceHealthProvider healthProvider)
        {
            var health = healthProvider.GetHealth();
            LastSourceError = health.LastError;
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
        LastSourceError = "manual-cache-injected";
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
