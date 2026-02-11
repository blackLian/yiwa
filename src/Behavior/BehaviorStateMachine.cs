using System;

namespace Companion.Behavior;

public sealed class BehaviorStateMachine
{
    public BehaviorState CurrentState { get; private set; } = BehaviorState.IdleWander;
    public CatPersonalityProfile Personality { get; }

    public event Action<BehaviorState>? OnStateChanged;

    public BehaviorStateMachine(CatPersonalityProfile personality)
    {
        Personality = personality;
    }

    public BehaviorDecision Dispatch(BehaviorEvent behaviorEvent)
    {
        var decision = ResolveDecision(behaviorEvent);
        if (decision.NextState != CurrentState)
        {
            CurrentState = decision.NextState;
            OnStateChanged?.Invoke(CurrentState);
        }

        return decision;
    }

    private BehaviorDecision ResolveDecision(BehaviorEvent behaviorEvent)
    {
        return behaviorEvent.Type switch
        {
            BehaviorEventType.OnUserClick when Personality.ClickFriendlyBias >= 0
                => new(BehaviorState.ReactToClick, ReactionAnimation.SoftJump, "friendly-click"),

            BehaviorEventType.OnUserClick
                => new(BehaviorState.ReactToClick, ReactionAnimation.TurnAway, "cold-click"),

            BehaviorEventType.OnMouseNear when Personality.MouseApproachBias < 0
                => new(BehaviorState.AvoidMouse, ReactionAnimation.BackStep, "avoid-mouse"),

            BehaviorEventType.OnMouseNear
                => new(BehaviorState.StayNearActiveRegion, ReactionAnimation.Blink, "approach-mouse"),

            BehaviorEventType.OnIconCollision when Personality.IconCuriosityBias > 0.2f
                => new(BehaviorState.CuriousAtIcon, ReactionAnimation.BubbleHint, $"icon:{behaviorEvent.IconName}"),

            BehaviorEventType.OnIdleTimer when behaviorEvent.IdleSeconds >= Personality.SleepThresholdSeconds
                => new(BehaviorState.Sleep, ReactionAnimation.SleepBreath, "idle-sleep"),

            _ => new(BehaviorState.IdleWander, ReactionAnimation.None, "default-wander"),
        };
    }
}
