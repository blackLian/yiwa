using System;
using Companion.App;
using Companion.Behavior;
using Companion.Desktop.Diagnostics;
using Companion.Desktop.Icons;

namespace Companion.Desktop.Runtime;

public enum RuntimeAdvisory
{
    None,
    UseCachedIcons,
    EnterRecoveryMode,
    NativePartialMapping,
}

public sealed record RuntimeTickResult(
    bool Executed,
    bool Succeeded,
    BehaviorState? NextState,
    string BubbleText,
    int IconCount,
    int PollingHz,
    int ConsecutiveFailures,
    RuntimeAdvisory Advisory,
    string Message);

public sealed record RuntimeHealthSnapshot(
    int PollingHz,
    int PreferredPollingHz,
    int ConsecutiveFailures,
    bool IsInRecoveryMode,
    int InfoLogCount,
    int WarnLogCount,
    IconSourceMode IconSourceMode,
    string LastSourceError);

public sealed class DesktopIntegrationRuntime
{
    private readonly IntegrationScheduler _scheduler;
    private readonly IDesktopIconProvider _iconProvider;
    private readonly InteractionOrchestrator _orchestrator;
    private readonly IntegrationDiagnostics _diagnostics;

    private DateTimeOffset _lastTickAt = DateTimeOffset.MinValue;

    public DesktopIntegrationRuntime(
        IntegrationScheduler scheduler,
        IDesktopIconProvider iconProvider,
        InteractionOrchestrator orchestrator,
        IntegrationDiagnostics diagnostics)
    {
        _scheduler = scheduler;
        _iconProvider = iconProvider;
        _orchestrator = orchestrator;
        _diagnostics = diagnostics;
    }

    public RuntimeTickResult Tick((float X, float Y) petPosition, DateTimeOffset now)
    {
        if (!_scheduler.ShouldRun(now, _lastTickAt))
        {
            return new RuntimeTickResult(
                Executed: false,
                Succeeded: true,
                NextState: null,
                BubbleText: string.Empty,
                IconCount: 0,
                PollingHz: _scheduler.PollingHz,
                ConsecutiveFailures: _scheduler.ConsecutiveFailures,
                Advisory: RuntimeAdvisory.None,
                Message: "tick skipped by scheduler");
        }

        _lastTickAt = now;

        try
        {
            var icons = _iconProvider.GetVisibleIcons();
            var (decision, bubble) = _orchestrator.OnPetMovedNearIcons(petPosition, icons);

            _scheduler.MarkSuccess();
            var advisory = ResolveAdvisory(_iconProvider, icons.Count);
            var msg = $"tick ok: icons={icons.Count}, state={decision.NextState}, advisory={advisory}, bubble={(string.IsNullOrEmpty(bubble) ? "none" : bubble)}";
            _diagnostics.Info("DesktopRuntime", msg);

            return new RuntimeTickResult(
                Executed: true,
                Succeeded: true,
                NextState: decision.NextState,
                BubbleText: bubble,
                IconCount: icons.Count,
                PollingHz: _scheduler.PollingHz,
                ConsecutiveFailures: _scheduler.ConsecutiveFailures,
                Advisory: advisory,
                Message: msg);
        }
        catch (Exception ex)
        {
            _scheduler.MarkFailure();
            var advisory = _scheduler.IsInRecoveryMode
                ? RuntimeAdvisory.EnterRecoveryMode
                : RuntimeAdvisory.None;
            var msg = $"tick failed: {ex.Message}; pollingHz={_scheduler.PollingHz}; failures={_scheduler.ConsecutiveFailures}; advisory={advisory}";
            _diagnostics.Warn("DesktopRuntime", msg);

            return new RuntimeTickResult(
                Executed: true,
                Succeeded: false,
                NextState: null,
                BubbleText: string.Empty,
                IconCount: 0,
                PollingHz: _scheduler.PollingHz,
                ConsecutiveFailures: _scheduler.ConsecutiveFailures,
                Advisory: advisory,
                Message: msg);
        }
    }

    public RuntimeHealthSnapshot GetHealthSnapshot()
    {
        return new RuntimeHealthSnapshot(
            PollingHz: _scheduler.PollingHz,
            PreferredPollingHz: _scheduler.PreferredPollingHz,
            ConsecutiveFailures: _scheduler.ConsecutiveFailures,
            IsInRecoveryMode: _scheduler.IsInRecoveryMode,
            InfoLogCount: _diagnostics.CountByLevel("Info"),
            WarnLogCount: _diagnostics.CountByLevel("Warn"),
            IconSourceMode: _iconProvider is DesktopIconProvider dip ? dip.LastSourceMode : IconSourceMode.Unknown,
            LastSourceError: _iconProvider is DesktopIconProvider dep ? dep.LastSourceError : string.Empty);
    }

    private static RuntimeAdvisory ResolveAdvisory(IDesktopIconProvider provider, int iconCount)
    {
        if (provider is DesktopIconProvider desktopProvider &&
            desktopProvider.LastSourceMode == IconSourceMode.NativePartial)
        {
            return RuntimeAdvisory.NativePartialMapping;
        }

        if (iconCount == 0 || provider is DesktopIconProvider p && p.LastSourceMode == IconSourceMode.CachedOnly)
        {
            return RuntimeAdvisory.UseCachedIcons;
        }

        return RuntimeAdvisory.None;
    }

    public bool TryTick((float X, float Y) petPosition, DateTimeOffset now, out string bubbleText)
    {
        var result = Tick(petPosition, now);
        bubbleText = result.BubbleText;
        return result.Executed;
    }
}
