using System;
using System.Linq;

namespace Companion.Asset;

public static class PetAssetProfileValidator
{
    public static void Validate(PetAssetProfile profile)
    {
        if (string.IsNullOrWhiteSpace(profile.Id))
        {
            throw new InvalidOperationException("profile.id is required");
        }

        if (string.IsNullOrWhiteSpace(profile.DisplayName))
        {
            throw new InvalidOperationException("profile.displayName is required");
        }

        if (profile.PoseAssets is null || profile.PoseAssets.Count < 3)
        {
            throw new InvalidOperationException("profile.poseAssets requires at least 3 assets");
        }

        if (profile.PoseAssets.Any(string.IsNullOrWhiteSpace))
        {
            throw new InvalidOperationException("profile.poseAssets contains empty value");
        }

        if (profile.Behavior is null)
        {
            throw new InvalidOperationException("profile.behavior is required");
        }

        ValidateProbability(profile.Behavior.IconCuriosityProbability, "behavior.iconCuriosityProbability");

        if (profile.Behavior.MouseApproachProbability > 0)
        {
            ValidateProbability(profile.Behavior.MouseApproachProbability, "behavior.mouseApproachProbability");
        }

        if (profile.Behavior.MouseAvoidProbability > 0)
        {
            ValidateProbability(profile.Behavior.MouseAvoidProbability, "behavior.mouseAvoidProbability");
        }
    }

    private static void ValidateProbability(float value, string field)
    {
        if (value is < 0 or > 1)
        {
            throw new InvalidOperationException($"{field} must be between 0 and 1");
        }
    }
}
