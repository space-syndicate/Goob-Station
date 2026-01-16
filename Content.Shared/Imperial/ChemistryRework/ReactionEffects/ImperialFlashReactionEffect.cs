using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;

namespace Content.Shared.Chemistry.ReactionEffects;


public sealed partial class ImperialFlashReactionEffect : EntityEffectBase<ImperialFlashReactionEffect>
{
    [DataField(required: true)]
    public float MaxRange = 10;

    [DataField]
    public TimeSpan MaxDuration = TimeSpan.FromSeconds(3.0f);

    [DataField]
    public float SlowTo = 0.8f;

    [DataField]
    public float PowerPerUnit = 0.25f;

    [DataField]
    public bool SlowOnlyTarget = false;

    /// <summary>
    ///     The prototype ID used for the visual effect.
    /// </summary>
    [DataField]
    public EntProtoId? FlashEffectPrototype = "ReactionFlash";


    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys) =>
        Loc.GetString("reagent-effect-guidebook-flash",
            ("chance", Probability)
        );
}
