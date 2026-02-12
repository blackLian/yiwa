namespace Companion.Behavior;

public sealed class CatPersonalityProfile
{
    public required string Id { get; init; }
    public required string DisplayName { get; init; }
    public required float MouseApproachBias { get; init; } // >0 更接近鼠标, <0 更回避鼠标
    public required float IconCuriosityBias { get; init; } // >0 更容易停留图标附近
    public required float ClickFriendlyBias { get; init; } // >0 更友好, <0 更高冷
    public required float SleepThresholdSeconds { get; init; }

    public static CatPersonalityProfile Cute => new()
    {
        Id = "cute",
        DisplayName = "可爱治愈猫",
        MouseApproachBias = 0.75f,
        IconCuriosityBias = 0.65f,
        ClickFriendlyBias = 0.8f,
        SleepThresholdSeconds = 35f,
    };

    public static CatPersonalityProfile Cool => new()
    {
        Id = "cool",
        DisplayName = "高冷傲娇猫",
        MouseApproachBias = -0.8f,
        IconCuriosityBias = 0.1f,
        ClickFriendlyBias = -0.7f,
        SleepThresholdSeconds = 22f,
    };
}
