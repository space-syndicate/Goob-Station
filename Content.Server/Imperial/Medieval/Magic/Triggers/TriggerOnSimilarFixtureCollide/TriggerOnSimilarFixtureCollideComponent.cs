using Robust.Shared.Audio;

namespace Content.Server.Imperial.Medieval.Magic.Triggers;

/// <summary>
/// Trigger when a else fixture with same ID collide
/// </summary>
[RegisterComponent]
public sealed partial class TriggerOnSimilarFixtureCollideComponent : Component
{
    [DataField("fixtureID", required: true)]
    public string FixtureID = "";

    [DataField("otherFixtureID")]
    public string? OtherFixtureID;

}
