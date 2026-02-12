from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]


def must_contain(path: str, snippets: list[str]) -> None:
    text = (ROOT / path).read_text(encoding="utf-8")
    for s in snippets:
        assert s in text, f"{path} missing snippet: {s}"


def main() -> None:
    must_contain(
        "src/Desktop/Win32/OverlayWindowHost.cs",
        [
            "bool IsClickThrough { get; }",
            "void SetClickThrough(bool enabled);",
            "IsTransparentBackground = enabled;",
        ],
    )

    must_contain(
        "src/Desktop/Win32/ClickThroughService.cs",
        [
            "LastSwitchDurationMs",
            "Stopwatch",
            "_host.SetClickThrough(enabled);",
        ],
    )

    must_contain(
        "src/Desktop/Runtime/IntegrationScheduler.cs",
        [
            "ConsecutiveFailures",
            "MinimumPollingHz",
            "ShouldRun(DateTimeOffset now, DateTimeOffset lastRunAt)",
            "IsInRecoveryMode => ConsecutiveFailures >= 3",
        ],
    )

    must_contain(
        "src/Desktop/Diagnostics/IntegrationDiagnostics.cs",
        [
            "record IntegrationLog",
            "public void Info",
            "public void Warn",
            "public int CountByLevel",
            "public IntegrationLog? LastOrDefault",
        ],
    )

    print("ok: p3 progress validations passed")


if __name__ == "__main__":
    main()
