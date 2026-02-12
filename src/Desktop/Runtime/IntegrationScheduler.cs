using System;

namespace Companion.Desktop.Runtime;

public sealed class IntegrationScheduler
{
    private const int MinimumPollingHz = 2;

    public int PollingHz { get; private set; } = 10;
    public int PreferredPollingHz { get; private set; } = 10;
    public int ConsecutiveFailures { get; private set; }
    public bool IsInRecoveryMode => ConsecutiveFailures >= 3;

    public void SetPollingHz(int hz)
    {
        PollingHz = Math.Clamp(hz, MinimumPollingHz, 30);
    }

    public void SetPreferredPollingHz(int hz)
    {
        PreferredPollingHz = Math.Clamp(hz, MinimumPollingHz, 30);
        SetPollingHz(PreferredPollingHz);
    }

    public void MarkSuccess()
    {
        ConsecutiveFailures = 0;

        // 当上一次因失败降频后，恢复到用户设定频率。
        if (PollingHz < PreferredPollingHz)
        {
            SetPollingHz(PreferredPollingHz);
        }
    }

    public void MarkFailure()
    {
        ConsecutiveFailures++;
        if (IsInRecoveryMode)
        {
            // 回滚策略：连续失败后降频。
            SetPollingHz(MinimumPollingHz);
        }
    }

    public TimeSpan CurrentInterval => TimeSpan.FromMilliseconds(1000d / PollingHz);

    public bool ShouldRun(DateTimeOffset now, DateTimeOffset lastRunAt)
    {
        return now - lastRunAt >= CurrentInterval;
    }
}
