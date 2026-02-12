using System;
using System.Collections.Generic;

namespace Companion.Desktop.Icons;

public sealed class RetryingDesktopIconSource : IDesktopIconSource, IDesktopIconSourceHealthProvider
{
    private readonly IDesktopIconSource _inner;
    private readonly TimeSpan _retryWindow;

    private DateTimeOffset _lastAttemptAt = DateTimeOffset.MinValue;
    private int _consecutiveFailures;
    private string _lastError = string.Empty;

    public RetryingDesktopIconSource(IDesktopIconSource inner, TimeSpan? retryWindow = null)
    {
        _inner = inner;
        _retryWindow = retryWindow ?? TimeSpan.FromMilliseconds(500);
    }

    public bool TryGetVisibleIcons(out IReadOnlyList<DesktopIconInfo> icons)
    {
        var now = DateTimeOffset.UtcNow;
        if (_lastAttemptAt != DateTimeOffset.MinValue && now - _lastAttemptAt < _retryWindow)
        {
            icons = Array.Empty<DesktopIconInfo>();
            return false;
        }

        _lastAttemptAt = now;
        try
        {
            var ok = _inner.TryGetVisibleIcons(out icons);
            if (ok)
            {
                _consecutiveFailures = 0;
                _lastError = string.Empty;
                return true;
            }

            _consecutiveFailures++;
            _lastError = "source returned unavailable";
            return false;
        }
        catch (Exception ex)
        {
            _consecutiveFailures++;
            _lastError = ex.Message;
            icons = Array.Empty<DesktopIconInfo>();
            return false;
        }
    }

    public DesktopIconSourceHealth GetHealth()
    {
        return new DesktopIconSourceHealth(
            IsReady: _consecutiveFailures == 0,
            ConsecutiveFailures: _consecutiveFailures,
            LastAttemptAt: _lastAttemptAt,
            LastError: _lastError);
    }
}
