using Robust.Shared.GameStates;

namespace Content.Shared.Imperial.Traits;

/// <summary>
/// A disabled person can crawl slowly because he has arms!
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(SharedImperialLegsParalyzedSystem))]
public sealed partial class ImperialLegsParalyzedComponent : Component
{
    /// <summary>
    /// Tracks whether added a crawl state
    /// </summary>
    [DataField, ViewVariables]
    public bool AddedKnockdown = false;

    [DataField, ViewVariables]
    public float CrawlSpeed = 0.9f;

    [DataField, ViewVariables]
    public float CrawlAcceleration = 25f;
}
