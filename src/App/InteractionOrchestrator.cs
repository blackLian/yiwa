using System.Collections.Generic;
using Companion.Behavior;
using Companion.Desktop;
using Companion.Desktop.Icons;

namespace Companion.App;

public sealed class InteractionOrchestrator
{
    private readonly BehaviorStateMachine _stateMachine;
    private readonly IconCollisionService _iconCollisionService;

    public InteractionOrchestrator(
        BehaviorStateMachine stateMachine,
        IconCollisionService iconCollisionService)
    {
        _stateMachine = stateMachine;
        _iconCollisionService = iconCollisionService;
    }

    public BehaviorDecision OnMouseNear(float distance)
    {
        return _stateMachine.Dispatch(new BehaviorEvent(BehaviorEventType.OnMouseNear, Distance: distance));
    }

    public BehaviorDecision OnUserClick(float clickRate)
    {
        return _stateMachine.Dispatch(new BehaviorEvent(BehaviorEventType.OnUserClick, ClickRate: clickRate));
    }

    public BehaviorDecision OnIdle(float idleSeconds)
    {
        return _stateMachine.Dispatch(new BehaviorEvent(BehaviorEventType.OnIdleTimer, IdleSeconds: idleSeconds));
    }

    public (BehaviorDecision Decision, string BubbleText) OnPetMovedNearIcon(
        (float X, float Y) petPosition,
        string iconName,
        (float X, float Y, float W, float H) iconRect)
    {
        var collision = _iconCollisionService.IsVisualCollision(petPosition, iconRect);
        if (!collision)
        {
            return (_stateMachine.Dispatch(new BehaviorEvent(BehaviorEventType.OnIdleTimer)), string.Empty);
        }

        var decision = _stateMachine.Dispatch(new BehaviorEvent(
            BehaviorEventType.OnIconCollision,
            IconName: iconName));
        var bubble = _iconCollisionService.BuildCollisionBubbleText(iconName);
        return (decision, bubble);
    }

    public (BehaviorDecision Decision, string BubbleText) OnPetMovedNearIcons(
        (float X, float Y) petPosition,
        IReadOnlyList<DesktopIconInfo> icons)
    {
        foreach (var icon in icons)
        {
            if (!_iconCollisionService.IsVisualCollision(petPosition, icon))
            {
                continue;
            }

            var decision = _stateMachine.Dispatch(new BehaviorEvent(
                BehaviorEventType.OnIconCollision,
                IconName: icon.Name));
            return (decision, _iconCollisionService.BuildCollisionBubbleText(icon.Name));
        }

        return (_stateMachine.Dispatch(new BehaviorEvent(BehaviorEventType.OnIdleTimer)), string.Empty);
    }
}
