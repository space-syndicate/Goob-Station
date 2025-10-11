using System.Linq;
using System.Numerics;
using Content.Server.Explosion.EntitySystems;
using Content.Shared.EntityEffects;
using Content.Shared.Imperial.ShockWave;
using Content.Shared.Trigger;
using Robust.Server.GameObjects;
using Robust.Server.GameStates;
using Robust.Shared.Timing;

namespace Content.Server.Imperial.ShockWave;


public sealed class ShockWaveSystem : SharedShockWaveSystem
{
    [Dependency] private readonly PvsOverrideSystem _pvs = default!;
    [Dependency] private readonly EntityLookupSystem _lookupSystem = default!;
    [Dependency] private readonly TransformSystem _transformSystem = default!;
    [Dependency] private readonly IGameTiming _timing = default!;


    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ShockWaveComponent, ComponentStartup>(OnInit);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var enumerator = EntityQueryEnumerator<ShockWaveComponent>();

        while (enumerator.MoveNext(out var uid, out var component))
        {
            if (component.NextUpdate >= _timing.CurTime) continue;

            component.NextUpdate = _timing.CurTime + component.UpdateRate;

            var time = (float)(_timing.CurTime - component.SpawnTime).TotalMilliseconds;
            var radius = time * component.Speed * 0.01f;

            if (radius <= 0) continue;

            var collidedEntities = _lookupSystem.GetEntitiesInRange(uid, radius, component.CollideFlags);

            foreach (var entity in collidedEntities)
            {
                if (component.CollidedEntities.Contains(entity)) continue;
                var position = _transformSystem.GetWorldPosition(entity);
                var wavePosition = _transformSystem.GetWorldPosition(uid);

                if (!EntityInRange(wavePosition, position, radius - component.BorderWidth)) continue;

                component.CollidedEntities.Add(entity);

                var triggerEv = new TriggerEvent(entity);

                RaiseLocalEvent(uid, new ShockWaveEntityCollideEvent(uid, entity));
                RaiseLocalEvent(uid, ref triggerEv, true);

                foreach (var effect in component.Effects)
                {
                    var args = new EntityEffectBaseArgs(entity, EntityManager);
                    var canApplyEffect = effect.Conditions?.Aggregate(true, (acc, condition) => condition.Condition(args) && acc) ?? true;

                    if (!canApplyEffect) continue;

                    effect.Effect(args);
                }
            }
        }
    }

    private void OnInit(EntityUid uid, ShockWaveComponent component, ComponentStartup args)
    {
        component.SpawnTime = _timing.CurTime;

        _pvs.AddGlobalOverride(uid);
    }

    #region Helpers

    private bool EntityInRange(Vector2 v1, Vector2 v2, float range)
    {
        var dx = v1.X - v2.X;
        var dy = v1.Y - v2.Y;

        return Math.Abs(dx * dx + dy * dy) > range * range;
    }

    #endregion
}
