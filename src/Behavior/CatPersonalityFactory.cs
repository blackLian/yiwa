using Companion.Asset;

namespace Companion.Behavior;

public static class CatPersonalityFactory
{
    public static CatPersonalityProfile FromAsset(PetAssetProfile profile)
    {
        var behavior = profile.Behavior;
        var approach = behavior.MouseApproachProbability > 0
            ? behavior.MouseApproachProbability
            : -behavior.MouseAvoidProbability;

        var clickBias = behavior.ClickGreetingProbability > 0
            ? behavior.ClickGreetingProbability
            : -behavior.ClickWalkAwayProbability;

        return new CatPersonalityProfile
        {
            Id = profile.Id,
            DisplayName = profile.DisplayName,
            MouseApproachBias = approach,
            IconCuriosityBias = behavior.IconCuriosityProbability,
            ClickFriendlyBias = clickBias,
            SleepThresholdSeconds = profile.Id == "cool" ? 22f : 35f,
        };
    }
}
