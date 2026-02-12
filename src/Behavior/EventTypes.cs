namespace Companion.Behavior;

public enum BehaviorState
{
    IdleWander,
    StayNearActiveRegion,
    Sleep,
    AvoidMouse,
    CuriousAtIcon,
    ReactToClick
}

public enum BehaviorEventType
{
    OnMouseNear,
    OnIconCollision,
    OnIdleTimer,
    OnUserClick
}

public sealed record BehaviorEvent(
    BehaviorEventType Type,
    float Distance = 0,
    string IconName = "",
    float IdleSeconds = 0,
    float ClickRate = 0);
