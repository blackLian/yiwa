from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]


def contains(path: str, expected: list[str]) -> None:
    text = (ROOT / path).read_text(encoding="utf-8")
    for snippet in expected:
        assert snippet in text, f"{path} missing: {snippet}"


def main() -> None:
    contains(
        "src/Desktop/Runtime/DesktopIntegrationRuntime.cs",
        [
            "enum RuntimeAdvisory",
            "record RuntimeHealthSnapshot",
            "ConsecutiveFailures",
            "public RuntimeHealthSnapshot GetHealthSnapshot()",
            "RuntimeAdvisory.UseCachedIcons",
            "RuntimeAdvisory.EnterRecoveryMode",
            "ResolveAdvisory(IDesktopIconProvider provider, int iconCount)",
            "RuntimeAdvisory.NativePartialMapping",
        ],
    )

    contains(
        "src/Desktop/Icons/DesktopIconProvider.cs",
        [
            "public bool LastRefreshSucceeded",
            "public int CachedCount => _cached.Count",
            "class EmptyDesktopIconSource",
        ],
    )

    contains(
        "src/Desktop/Runtime/IntegrationScheduler.cs",
        [
            "IsInRecoveryMode => ConsecutiveFailures >= 3",
            "SetPreferredPollingHz",
            "if (PollingHz < PreferredPollingHz)",
        ],
    )

    contains(
        "tests/p3_smoke_test.ps1",
        [
            "Summary: all checks passed",
            "validate_behavior",
            "validate_runtime_operator_progress",
        ],
    )

    print("ok: desktop runtime progress validated")


if __name__ == "__main__":
    main()
