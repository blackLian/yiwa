using System;

namespace Companion.Desktop.Icons;

public sealed record DesktopIconSourceHealth(
    bool IsReady,
    int ConsecutiveFailures,
    DateTimeOffset LastAttemptAt,
    string LastError = "");

public interface IDesktopIconSourceHealthProvider
{
    DesktopIconSourceHealth GetHealth();
}
