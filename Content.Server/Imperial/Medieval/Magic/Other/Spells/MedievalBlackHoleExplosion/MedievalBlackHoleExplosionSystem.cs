using System.Numerics;
using Content.Server.Explosion.EntitySystems;
using Content.Shared.Imperial.Medieval.Magic.ProjectileCursorFollower;
using Content.Shared.Singularity.Components;
using Content.Shared.Trigger;
using Robust.Server.GameObjects;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Spawners;
using Robust.Shared.Timing;

namespace Content.Server.Imperial.Medieval.Magic.MedievalBlackHoleExplosion;


/// <summary>
///
/// </summary>
public sealed partial class MedievalBlackHoleExplosionSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedPhysicsSystem _physicsSystem = default!;
    [Dependency] private readonly ExplosionSystem _explosionSystem = default!;
    [Dependency] private readonly PointLightSystem _pointLightSystem = default!;


    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MedievalBlackHoleExplosionComponent, TriggerEvent>(OnTrigger);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var enumerator = EntityQueryEnumerator<MedievalBlackHoleExplosionComponent, SingularityDistortionComponent, PointLightComponent>();

        while (enumerator.MoveNext(out var uid, out var component, out var singularityDistortionComponent, out var pointLightComponent))
        {
            if (!component.Active) continue;
            if (component.WhenExplode < _timing.CurTime)
            {
                _explosionSystem.TriggerExplosive(uid);

                continue;
            }

            if (_timing.CurTime <= component.NextExplodeUpdate) return;

            if (component.InitialColor != null)
            {
                var colorStep = GetColorStep(component.InitialColor.Value, component.TargetColor, component.ExplodeTime);
                var step = component.ExplodeTime - (component.WhenExplode - _timing.CurTime);

                _pointLightSystem.SetColor(uid, Color.FromXyz(ToRobustVector4(component.InitialColor.Value.RGBA) + colorStep * (float)step.TotalSeconds), pointLightComponent);
                _pointLightSystem.SetEnergy(uid, pointLightComponent.Energy + component.BlackHoleLightEnergyPerTick, pointLightComponent);
                _pointLightSystem.SetRadius(uid, pointLightComponent.Radius + component.BlackHoleLightRangePerTick, pointLightComponent);
            }

            component.NextExplodeUpdate = _timing.CurTime + component.ExplodeUpdateRate;

#pragma warning disable RA0002
            singularityDistortionComponent.FalloffPower -= component.BlackHoleSizePerTick;
#pragma warning restore RA0002

            Dirty(uid, singularityDistortionComponent);
        }
    }

    private void OnTrigger(EntityUid uid, MedievalBlackHoleExplosionComponent component, TriggerEvent args)
    {
        QueueDel(args.User);

        RemComp<ProjectileCursorFollowerComponent>(uid);
        RemComp<TimedDespawnComponent>(uid);

        component.Active = true;
        component.WhenExplode = _timing.CurTime + component.ExplodeTime;
        component.NextExplodeUpdate = _timing.CurTime + component.ExplodeUpdateRate;

        if (TryComp<PointLightComponent>(uid, out var lightComponent))
            component.InitialColor = lightComponent.Color;

        _physicsSystem.SetLinearVelocity(uid, Vector2.Zero);
    }

    #region Helpers

    private Vector4 GetColorStep(Color initialColor, Color targetColor, TimeSpan time)
    {
        return new Vector4(
            (targetColor.R - initialColor.R) / (float)time.TotalSeconds,
            (targetColor.G - initialColor.G) / (float)time.TotalSeconds,
            (targetColor.B - initialColor.B) / (float)time.TotalSeconds,
            (targetColor.A - initialColor.A) / (float)time.TotalSeconds
        );
    }

    private Vector4 ToRobustVector4(System.Numerics.Vector4 vector4) => new Vector4(vector4.X, vector4.Y, vector4.Z, vector4.W);

    #endregion
}
