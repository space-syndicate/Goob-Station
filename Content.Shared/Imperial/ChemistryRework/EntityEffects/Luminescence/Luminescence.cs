using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;

namespace Content.Shared.Chemistry.ReactionEffects;


/// <summary>
///     Causes the entity to glow. Works via metabolism and via <c>reactiveEffects</c> with <see cref="ReactionMethod.Touch"/>.
/// </summary>
public sealed partial class Luminescence : EntityEffectBase<Luminescence>
{
    /// <summary>
    ///     If the color was not transferred, then generates a random color every metabolization cycle
    /// </summary>
    [DataField("color")]
    public Color? PaintingColor;

    [DataField]
    public float MinEnergy = 2;

    [DataField]
    public float MaxEnergy = float.PositiveInfinity;

    [DataField]
    public float MinRange = 2;

    [DataField]
    public float MaxRange = float.PositiveInfinity;

    [DataField]
    public float RangePerUnit = 0.1f;

    [DataField]
    public float EnergyPerUnit = 0.1f;

    /// <summary>
    /// Extra glow duration per reagent unit when applied through <see cref="ReactionMethod.Touch"/> (splash on skin, etc.).
    /// </summary>
    [DataField]
    public float TouchExpiryPerUnit;

    [DataField]
    public TimeSpan GlowExpiryGrace = TimeSpan.FromSeconds(3);


    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys) =>
        Loc.GetString("reagent-effect-guidebook-luminescence",
            ("chance", Probability)
        );
}
