using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;

namespace Content.Shared.Chemistry.ReactionEffects;

/// <summary>
///     Creates a gravitational pulse, shoving around all entities within some distance of an epicenter.
/// </summary>
public sealed partial class GravityReactionEffect : EntityEffectBase<GravityReactionEffect>
{
    [DataField]
    public float MinRange = default!;

    [DataField(required: true)]
    public float MaxRange = default!;

    /// <summary>
    ///     The base radial velocity that will be added to entities within range towards the center of the gravitational pulse
    /// </summary>
    [DataField]
    public float BaseRadialDeltaV = default!;

    /// <summary>
    ///     The base tangential velocity that will be added to entities within countrclockwise around the center of the gravitational pulse.
    /// </summary>
    [DataField]
    public float BaseTangentialDeltaV = default!;

    [DataField]
    public float ImpulsePerUnit = 0.1f;

    public override string EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys) =>
        Loc.GetString("reagent-effect-guidebook-gravity",
            ("chance", Probability),
            ("type", BaseRadialDeltaV > 0 ? "pulls" : "repels")
        );
}
