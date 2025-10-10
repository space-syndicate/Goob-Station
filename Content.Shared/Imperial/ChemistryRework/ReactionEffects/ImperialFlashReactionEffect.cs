using Content.Shared.EntityEffects;
using Content.Shared.Flash;
using Robust.Shared.Prototypes;

namespace Content.Shared.Chemistry.ReactionEffects;


public sealed partial class ImperialFlashReactionEffect : EventEntityEffect<ImperialFlashReactionEffect>
{
    [DataField("maxRange", required: true)]
    public float MaxRange = 10;

    [DataField("maxDuration")]
    public TimeSpan MaxDuration = TimeSpan.FromSeconds(3.0f);

    [DataField("slowTo")]
    public float SlowTo = 0.8f;

    [DataField("powerPerUnit")]
    public float PowerPerUnit = 0.25f;

    [DataField]
    public bool SlowOnlyTarget = false;

    /// <summary>
    ///     The prototype ID used for the visual effect.
    /// </summary>
    [DataField]
    public EntProtoId? FlashEffectPrototype = "ReactionFlash";


    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys) =>
        Loc.GetString("reagent-effect-guidebook-flash",
            ("chance", Probability)
        );
}
