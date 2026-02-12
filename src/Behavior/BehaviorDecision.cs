namespace Companion.Behavior;

public enum ReactionAnimation
{
    None,
    SoftJump,
    Blink,
    TurnAway,
    BackStep,
    BubbleHint,
    SleepBreath
}

public sealed record BehaviorDecision(
    BehaviorState NextState,
    ReactionAnimation Animation,
    string Note = "");
