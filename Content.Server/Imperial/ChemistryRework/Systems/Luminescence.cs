using Content.Shared.Chemistry.ReactionEffects;
using Content.Shared.EntityEffects;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.Chemistry.ReactionEffects;


public sealed partial class LuminescenceEntityEffectSystem : EntityEffectSystem<MetaDataComponent, Luminescence>
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedPointLightSystem _pointLight = default!;


    protected override void Effect(Entity<MetaDataComponent> entity, ref EntityEffectEvent<Luminescence> args)
    {
        var light = _pointLight.EnsureLight(entity);
        var tracker = EnsureComp<LuminescenceTrackerComponent>(entity);
        tracker.Accumulated += args.Scale;

        _pointLight.SetColor(entity, GetColor(args), light);
        _pointLight.SetEnergy(entity, GetEnergy(args, tracker), light);
        _pointLight.SetRadius(entity, GetRadius(args, tracker), light);
        _pointLight.SetEnabled(entity, true, light);
        _pointLight.SetCastShadows(entity, true, light);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<LuminescenceTrackerComponent>();
        while (query.MoveNext(out var uid, out var tracker))
        {
            if (tracker.NextCheck >= _timing.CurTime)
                continue;

            tracker.Accumulated -= 1;
            tracker.NextCheck = _timing.CurTime + tracker.GracePeriod;

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
