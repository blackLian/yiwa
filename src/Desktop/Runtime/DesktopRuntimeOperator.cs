using System;
using Companion.Desktop.Diagnostics;

namespace Companion.Desktop.Runtime;

public enum RuntimeStatusLevel
{
    Healthy,
    Degraded,
    Recovery,
}

public enum RuntimeUserNotice
{
    None,
    IconInteractionUnavailable,
    RuntimeRecoveryMode,
    IconInteractionPartial,
    IconInteractionFallbackGrid,
}

public sealed record RuntimeUpdateResult(
    RuntimeTickResult Tick,
    RuntimeHealthSnapshot Health,
    RuntimeUserNotice Notice,
    string NoticeText,
    bool IsNoticeChanged,
    P3ProgressSnapshot Progress,
    double ProgressDeltaPercent,
    RuntimeStatusLevel StatusLevel,
    string RuntimeStatusSummary);

public sealed class DesktopRuntimeOperator
{
    private readonly DesktopIntegrationRuntime _runtime;
    private readonly IntegrationDiagnostics _diagnostics;
    private readonly IP3ProgressProbe? _progressProbe;
    private readonly bool _hasAcceptanceScript;
    private RuntimeUserNotice _lastNotice = RuntimeUserNotice.None;
    private double _lastCompletionPercent = -1d;

    public DesktopRuntimeOperator(
        DesktopIntegrationRuntime runtime,
        IntegrationDiagnostics diagnostics,
        IP3ProgressProbe? progressProbe = null,
        bool hasAcceptanceScript = true)
    {
        _runtime = runtime;
        _diagnostics = diagnostics;
        _progressProbe = progressProbe;
        _hasAcceptanceScript = hasAcceptanceScript;
    }

    public RuntimeUpdateResult Update((float X, float Y) petPosition, DateTimeOffset now)
    {
        var tick = _runtime.Tick(petPosition, now);
        var health = _runtime.GetHealthSnapshot();

        var (notice, text) = ResolveNotice(tick, health);
        var changed = notice != _lastNotice;
        _lastNotice = notice;

        // 仅在 notice 变化时记录 warning，避免每帧重复刷日志。
        if (notice != RuntimeUserNotice.None && changed)
        {
            _diagnostics.Warn("DesktopRuntimeNotice", text);
        }

        var progress = P3ProgressEstimator.Estimate(
            iconSourceReady: _progressProbe?.IsIconSourceReady() ?? false,
            hasAcceptanceScript: _hasAcceptanceScript);

        var delta = _lastCompletionPercent < 0d ? 0d : progress.CompletionPercent - _lastCompletionPercent;
        _lastCompletionPercent = progress.CompletionPercent;
        var statusLevel = ResolveStatusLevel(health, tick);

        var summary = BuildRuntimeStatusSummary(health, progress, delta, statusLevel);
        return new RuntimeUpdateResult(tick, health, notice, text, changed, progress, delta, statusLevel, summary);
    }

    private static RuntimeStatusLevel ResolveStatusLevel(RuntimeHealthSnapshot health, RuntimeTickResult tick)
    {
        if (health.IsInRecoveryMode || tick.Advisory == RuntimeAdvisory.EnterRecoveryMode)
        {
            return RuntimeStatusLevel.Recovery;
        }

        if (tick.Advisory is RuntimeAdvisory.UseCachedIcons or RuntimeAdvisory.NativePartialMapping or RuntimeAdvisory.FallbackGridMapping)
        {
            return RuntimeStatusLevel.Degraded;
        }

        return RuntimeStatusLevel.Healthy;
    }

    private static string BuildRuntimeStatusSummary(
        RuntimeHealthSnapshot health,
        P3ProgressSnapshot progress,
        double delta,
        RuntimeStatusLevel statusLevel)
    {
        return $"status={statusLevel}, progress={progress.CompletionPercent:0.0}% (Δ{delta:+0.0;-0.0;0.0}, {progress.CompletedIssueCount}/8), source={health.IconSourceMode}, advisories[cached={health.AdvisoryUseCachedCount},partial={health.AdvisoryNativePartialCount},fallback={health.AdvisoryFallbackGridCount},recovery={health.AdvisoryRecoveryCount}]";
    }

    private static (RuntimeUserNotice, string) ResolveNotice(RuntimeTickResult tick, RuntimeHealthSnapshot health)
    {
        if (health.IsInRecoveryMode || tick.Advisory == RuntimeAdvisory.EnterRecoveryMode)
        {
            return (RuntimeUserNotice.RuntimeRecoveryMode, "运行中出现连续失败，已进入恢复模式并降频。");
        }

        if (tick.Advisory == RuntimeAdvisory.UseCachedIcons)
        {
            return (RuntimeUserNotice.IconInteractionUnavailable, $"图标交互暂不可用，正在使用缓存/降级模式（source={health.IconSourceMode}）。");
        }

        if (tick.Advisory == RuntimeAdvisory.NativePartialMapping)
        {
            return (RuntimeUserNotice.IconInteractionPartial, $"图标交互部分可用：native 数据不完整，已自动补全回退网格（lastError={health.LastSourceError}）。");
        }
        if (tick.Advisory == RuntimeAdvisory.FallbackGridMapping)
        {
            return (RuntimeUserNotice.IconInteractionFallbackGrid, $"图标交互正在使用回退网格映射（source={health.IconSourceMode}，lastError={health.LastSourceError}）。");
        }

        return (RuntimeUserNotice.None, string.Empty);
    }
}
