using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared.Chemistry.ReactionEffects;


public sealed partial class LuminescenceEntityEffectSystem : EntityEffectSystem<MetaDataComponent, Luminescence>
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly SharedPointLightSystem _pointLightSystem = default!;


    protected override void Effect(Entity<MetaDataComponent> entity, ref EntityEffectEvent<Luminescence> args)
    {
        // If the next time the reagent metabolization ends, we turn off the glow.

        Logger.Error(args.Effect.Scaling + "");
    }

    private Color GenerateRandomColor()
    {
        var r = _random.NextByte(255);
        var g = _random.NextByte(255);
        var b = _random.NextByte(255);

        return new Color(r, g, b);
    }

    private void SetLightColor(EntityUid entity, EntityEffectEvent<Luminescence> args)
    {
        var color = args.Effect.PaintingColor != null ? args.Effect.PaintingColor.Value : GenerateRandomColor();

        _pointLightSystem.SetColor(entity, color);
    }

    private void ScaleLightPower(EntityUid uid, EntityEffectEvent<Luminescence> args)
    {
        var reagentCount = args.Scale;

        var energy = MathF.Max(
            MathF.Min((float)(reagentCount * args.Effect.EnergyPerUnit), args.Effect.MaxEnergy),
            args.Effect.MinEnergy
        );
        var range = MathF.Max(
            MathF.Min((float)(reagentCount * args.Effect.EnergyPerUnit), args.Effect.MaxEnergy),
            args.Effect.MinRange
        );

        _pointLightSystem.SetEnergy(uid, energy);
        _pointLightSystem.SetRadius(uid, range);
    }
}


/// <summary>
///     Causes the entity to glow.
/// </summary>
/// <remarks>
///     Since I made a crutch again, it does not work when applying the essence to the skin.
/// </remarks>
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

    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys) =>
        Loc.GetString("reagent-effect-guidebook-luminescence",
            ("chance", Probability)
        );
}
