using Content.Shared.EntityEffects;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared.Chemistry.ReactionEffects;


public sealed partial class LuminescenceEntityEffectSystem : EntityEffectSystem<MetaDataComponent, Luminescence>
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedPointLightSystem _pointLight = default!;


    protected override void Effect(Entity<MetaDataComponent> entity, ref EntityEffectEvent<Luminescence> args)
    {
        var light = _pointLight.EnsureLight(entity);
        var tracker = EnsureComp<LuminescenceTrackerComponent>(entity);

        _pointLight.SetColor(entity, GetColor(args), light);
        _pointLight.SetEnergy(entity, GetEnergy(args, tracker), light);
        _pointLight.SetRadius(entity, GetRadius(args, tracker), light);
        _pointLight.SetEnabled(entity, true, light);
        _pointLight.SetCastShadows(entity, true, light);

        if (!_timing.IsFirstTimePredicted)
            return;

        tracker.Accumulated += 1;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<LuminescenceTrackerComponent>();
        while (query.MoveNext(out var uid, out var tracker))
        {
            tracker.Accumulated -= 0;

            if (tracker.Accumulated > 0)
                continue;

            _pointLight.RemoveLightDeferred(uid);
            RemComp<LuminescenceTrackerComponent>(uid);
        }
    }

    #region Helpers

    private Color GetColor(EntityEffectEvent<Luminescence> args)
    {
        if (args.Effect.PaintingColor != null)
            return args.Effect.PaintingColor.Value;

        if (_net.IsClient)
            return Color.White;

        return GenerateRandomColor();
    }

    private Color GenerateRandomColor()
    {
        var r = _random.NextByte(255);
        var g = _random.NextByte(255);
        var b = _random.NextByte(255);

        return new Color(r, g, b);
    }

    private static float GetEnergy(EntityEffectEvent<Luminescence> args, LuminescenceTrackerComponent component)
    {
        var effect = args.Effect;
        return Math.Clamp(component.Accumulated * effect.EnergyPerUnit, effect.MinEnergy, effect.MaxEnergy);
    }

    private static float GetRadius(EntityEffectEvent<Luminescence> args, LuminescenceTrackerComponent component)
    {
        var effect = args.Effect;
        return Math.Clamp(component.Accumulated * effect.RangePerUnit, effect.MinRange, effect.MaxRange);
    }

    #endregion
}

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
