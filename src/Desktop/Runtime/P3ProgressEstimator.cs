namespace Companion.Desktop.Runtime;

public sealed record P3ProgressSnapshot(
    bool Issue01WindowHostDone,
    bool Issue02ClickThroughDone,
    bool Issue03IconSourceDone,
    bool Issue04IconCollisionRoutingDone,
    bool Issue05DpiTransformDone,
    bool Issue06SchedulerBackoffDone,
    bool Issue07DiagnosticsNoticeDone,
    bool Issue08AcceptanceScriptDone)
{
    public int CompletedIssueCount
    {
        get
        {
            var done = 0;
            if (Issue01WindowHostDone) done++;
            if (Issue02ClickThroughDone) done++;
            if (Issue03IconSourceDone) done++;
            if (Issue04IconCollisionRoutingDone) done++;
            if (Issue05DpiTransformDone) done++;
            if (Issue06SchedulerBackoffDone) done++;
            if (Issue07DiagnosticsNoticeDone) done++;
            if (Issue08AcceptanceScriptDone) done++;
            return done;
        }
    }

    public double CompletionPercent => CompletedIssueCount / 8d * 100d;
}

public static class P3ProgressEstimator
{
    public static P3ProgressSnapshot Estimate(
        bool iconSourceReady,
        bool hasAcceptanceScript)
    {
        return new P3ProgressSnapshot(
            Issue01WindowHostDone: true,
            Issue02ClickThroughDone: true,
            Issue03IconSourceDone: iconSourceReady,
            Issue04IconCollisionRoutingDone: true,
            Issue05DpiTransformDone: true,
            Issue06SchedulerBackoffDone: true,
            Issue07DiagnosticsNoticeDone: true,
            Issue08AcceptanceScriptDone: hasAcceptanceScript);
    }
}
