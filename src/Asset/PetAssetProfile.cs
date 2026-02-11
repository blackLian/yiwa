using System.Collections.Generic;

namespace Companion.Asset;

public sealed class PetAssetProfile
{
    public required string Id { get; init; }
    public required string DisplayName { get; init; }
    public required IReadOnlyList<string> PoseAssets { get; init; }
    public required PetBehaviorConfig Behavior { get; init; }
}

public sealed class PetBehaviorConfig
{
    public float MouseApproachProbability { get; init; }
    public float MouseAvoidProbability { get; init; }
    public float IconCuriosityProbability { get; init; }
    public float ClickGreetingProbability { get; init; }
    public float ClickWalkAwayProbability { get; init; }
}
