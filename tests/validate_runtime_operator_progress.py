from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]


def contains(path: str, snippets: list[str]) -> None:
    text = (ROOT / path).read_text(encoding="utf-8")
    for snippet in snippets:
        assert snippet in text, f"{path} missing: {snippet}"


def main() -> None:
    contains(
        "src/Desktop/Runtime/DesktopRuntimeOperator.cs",
        [
            "record RuntimeUpdateResult",
            "bool IsNoticeChanged",
            "P3ProgressSnapshot Progress",
            "progress={progress.CompletionPercent:0.0}%",
            "BuildRuntimeStatusSummary",
            "RuntimeStatusSummary",
            "status={statusLevel}",
            "ResolveStatusLevel",
            "ProgressDeltaPercent",
            "RuntimeStatusLevel",
            "private readonly IP3ProgressProbe? _progressProbe;",
            "iconSourceReady: _progressProbe?.IsIconSourceReady() ?? false",
            "if (notice != RuntimeUserNotice.None && changed)",
            "RuntimeAdvisory.NativePartialMapping",
            "IconInteractionPartial",
            "IconInteractionFallbackGrid",
            "RuntimeAdvisory.FallbackGridMapping",
            "source={health.IconSourceMode}",
            "lastError={health.LastSourceError}",
        ],
    )

    contains(
        "src/Desktop/Runtime/P3ProgressEstimator.cs",
        [
            "record P3ProgressSnapshot",
            "public int CompletedIssueCount",
            "public double CompletionPercent => CompletedIssueCount / 8d * 100d;",
        ],
    )

    contains(
        "src/Desktop/Runtime/P3ProgressProbe.cs",
        [
            "interface IP3ProgressProbe",
            "class P3ProgressProbe",
            "health.GetHealth().IsReady",
            "return _provider.LastRefreshSucceeded;",
        ],
    )


    contains(
        "src/Desktop/Icons/DesktopIconSourceHealth.cs",
        [
            "record DesktopIconSourceHealth",
            "interface IDesktopIconSourceHealthProvider",
        ],
    )

    contains(
        "src/Desktop/Icons/RetryingDesktopIconSource.cs",
        [
            "class RetryingDesktopIconSource",
            "IDesktopIconSourceHealthProvider",
            "GetHealth()",
            "_consecutiveFailures",
        ],
    )

    contains(
        "src/Desktop/Icons/Win32ExplorerIconSource.cs",
        [
            "class Win32ExplorerIconSource",
            "RuntimeInformation.IsOSPlatform",
            "IDesktopIconSourceHealthProvider",
            "Environment.SpecialFolder.DesktopDirectory",
            "FindWindowEx",
            "LVM_GETITEMCOUNT",
            "SystemParametersInfo",
            "SPI_GETWORKAREA",
            "ClientToScreen",
            "VirtualAllocEx",
            "ReadProcessMemory",
            "LVM_GETITEMPOSITION",
            "LVM_GETITEMTEXTW",
            "WriteProcessMemory",
            "OrderBy(static i => i.X)",
            "fallbackNames",
            "NormalizeNativeOrder",
            "current with { Name = candidate }",
            "used.Contains(candidate)",
            "HashSet<string>(StringComparer.OrdinalIgnoreCase)",
            "remainingFallbackNames",
            "Queue<string>(fallbackNames)",
            "TakeNextAvailableFallbackName",
            "fallbackGrid = MapToRuntimeGrid",
            "native-partial-mapping-active",
            "MergeNativeWithFallback",
        ],
    )

    contains(
        "src/Desktop/Icons/DesktopIconProviderFactory.cs",
        [
            "class DesktopIconProviderFactory",
            "new Win32ExplorerIconSource()",
            "new RetryingDesktopIconSource",
        ],
    )
    contains(
        "src/Desktop/Icons/DesktopIconProvider.cs",
        [
            "public int SourceFailureCount",
            "public int ConsecutiveSourceFailures",
            "public string LastSourceError",
            "public IconSourceMode LastSourceMode",
            "ConsecutiveSourceFailures = 0;",
            "ConsecutiveSourceFailures++;",
            "IDesktopIconSourceHealthProvider",
            "healthProvider.GetHealth()",
        ],
    )

    print("ok: runtime operator progress validated")


if __name__ == "__main__":
    main()
