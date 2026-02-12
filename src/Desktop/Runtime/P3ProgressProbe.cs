using Companion.Desktop.Icons;

namespace Companion.Desktop.Runtime;

public interface IP3ProgressProbe
{
    bool IsIconSourceReady();
}

public sealed class P3ProgressProbe : IP3ProgressProbe
{
    private readonly DesktopIconProvider _provider;
    private readonly IDesktopIconSource? _source;

    public P3ProgressProbe(DesktopIconProvider provider, IDesktopIconSource? source = null)
    {
        _provider = provider;
        _source = source;
    }

    public bool IsIconSourceReady()
    {
        // 首选：真实 source health（如果可用）
        if (_source is IDesktopIconSourceHealthProvider health)
        {
            return health.GetHealth().IsReady;
        }

        // 回退：provider 的最近刷新结果
        return _provider.LastRefreshSucceeded;
    }
}
