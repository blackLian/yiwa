using System;
using System.Collections.Generic;
using System.Linq;

namespace Companion.Desktop.Diagnostics;

public sealed record IntegrationLog(
    DateTimeOffset Timestamp,
    string Category,
    string Message,
    string Level = "Info");

public sealed class IntegrationDiagnostics
{
    private readonly List<IntegrationLog> _logs = new();

    public IReadOnlyList<IntegrationLog> Logs => _logs;

    public void Info(string category, string message)
    {
        _logs.Add(new IntegrationLog(DateTimeOffset.UtcNow, category, message, "Info"));
    }

    public void Warn(string category, string message)
    {
        _logs.Add(new IntegrationLog(DateTimeOffset.UtcNow, category, message, "Warn"));
    }

    public int CountByLevel(string level)
    {
        return _logs.Count(x => string.Equals(x.Level, level, StringComparison.OrdinalIgnoreCase));
    }

    public IntegrationLog? LastOrDefault(string category)
    {
        return _logs.LastOrDefault(x => string.Equals(x.Category, category, StringComparison.OrdinalIgnoreCase));
    }
}
