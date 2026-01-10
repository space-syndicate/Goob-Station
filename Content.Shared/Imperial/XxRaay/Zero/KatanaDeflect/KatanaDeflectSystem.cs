using System.Numerics;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Physics;
using Content.Shared.Projectiles;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Weapons.Reflect;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using System.Diagnostics.CodeAnalysis;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Timing;

namespace Content.Shared.Imperial.XxRaay.Zero.KatanaDeflect;

/// <summary>
/// System that handles katana projectile deflection mechanics.
/// When a katana with KatanaDeflectComponent attacks, it temporarily enables ReflectComponent
/// to deflect projectiles in the attack direction.
/// </summary>
public sealed class KatanaDeflectSystem : EntitySystem
{
    [Dependency] protected readonly IGameTiming GameTiming = default!;
    [Dependency] protected readonly SharedTransformSystem TransformSystem = default!;
    [Dependency] protected readonly SharedPhysicsSystem PhysicsSystem = default!;
    [Dependency] protected readonly EntityLookupSystem LookupSystem = default!;
    [Dependency] protected readonly SharedAudioSystem AudioSystem = default!;
    [Dependency] protected readonly SharedContainerSystem ContainerSystem = default!;
    [Dependency] protected readonly SharedProjectileSystem ProjectileSystem = default!;
    [Dependency] protected readonly ReflectSystem ReflectSystem = default!;
    [Dependency] protected readonly SharedHandsSystem HandsSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        
        SubscribeLocalEvent<KatanaDeflectComponent, ProjectileReflectAttemptEvent>(OnProjectileReflectAttempt);
        
        SubscribeAllEvent<HeavyAttackEvent>(OnHeavyAttack);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var currentTime = GameTiming.CurTime;
        var query = EntityQueryEnumerator<KatanaDeflectComponent, ReflectComponent>();
        
        while (query.MoveNext(out var uid, out var comp, out var reflect))
        {
            if (comp.LastAttackTime == TimeSpan.Zero)
                continue;

            var window = TimeSpan.FromSeconds(comp.ActiveWindow);
            if (currentTime > comp.LastAttackTime + window)
            {
                comp.LastAttackTime = TimeSpan.Zero;
                comp.LastAttackDirection = null;
                comp.HasDeflected = false;
                reflect.ReflectProb = 0f;
                Dirty(uid, comp);
                Dirty(uid, reflect);
                continue;
            }
        }
    }

    private void OnHeavyAttack(HeavyAttackEvent ev, EntitySessionEventArgs args)
    {
        if (args.SenderSession.AttachedEntity is not { } user)
            return;

        if (TryGetKatanaWithDeflect(user, out var katana))
        {
            ActivateDeflection(katana);
        }
    }

    private void ActivateDeflection(Entity<KatanaDeflectComponent> entity)
    {
        var component = entity.Comp;
        var currentTime = GameTiming.CurTime;
        
        component.LastAttackTime = currentTime;
        component.LastAttackDirection = GetFacingAngle(entity.Owner);
        component.HasDeflected = false;
        
        if (TryComp<ReflectComponent>(entity, out var reflect))
        {
            reflect.ReflectProb = 1.0f;
            Dirty(entity, reflect);
        }
        
        Dirty(entity, component);
    }

    private bool TryGetKatanaWithDeflect(EntityUid user, [NotNullWhen(true)] out Entity<KatanaDeflectComponent> katana)
    {
        katana = default;
        
        if (TryComp<HandsComponent>(user, out var hands))
        {
            foreach (var held in HandsSystem.EnumerateHeld((user, hands)))
            {
                if (TryComp<KatanaDeflectComponent>(held, out var comp))
                {
                    katana = (held, comp);
                    return true;
                }
            }
        }
        
        return false;
    }

    private void OnProjectileReflectAttempt(Entity<KatanaDeflectComponent> entity, ref ProjectileReflectAttemptEvent ev)
    {
        if (ev.Cancelled) return;
        
        var component = entity.Comp;
        
        if (component.LastAttackTime == TimeSpan.Zero) return;
        if (component.HasDeflected) return;

        var window = TimeSpan.FromSeconds(component.ActiveWindow);
        if (GameTiming.CurTime > component.LastAttackTime + window)
            return;
        
        if (!IsProjectileInDeflectionCone(ev.ProjUid, entity.Owner, component))
            return;
        
        if (component.LastAttackDirection.HasValue)
            OverrideReflectionDirection(ev.ProjUid, component.LastAttackDirection.Value);
        
        component.HasDeflected = true;

        if (TryComp<ReflectComponent>(entity, out var reflect))
        {
            reflect.ReflectProb = 0f;
            Dirty(entity, reflect);
        }

        Dirty(entity, component);
    }

    private bool IsProjectileInDeflectionCone(EntityUid projectile, EntityUid wielder, KatanaDeflectComponent comp)
    {
        var projPos = TransformSystem.GetWorldPosition(projectile);
        var wielderPos = TransformSystem.GetWorldPosition(wielder);
        var toProjectile = projPos - wielderPos;

        if (toProjectile.LengthSquared() > comp.Radius * comp.Radius)
            return false;

        var angle = Angle.FromWorldVec(toProjectile);
        var attackAngle = comp.LastAttackDirection ?? Angle.Zero;
        var angleDiff = (angle - attackAngle).Reduced().FlipPositive();

        var maxDiff = comp.DeflectAngle / 2f;
        return angleDiff <= maxDiff;
    }

    private void OverrideReflectionDirection(EntityUid projectile, Angle attackAngle)
    {
        if (!TryComp<PhysicsComponent>(projectile, out var physics))
            return;
            
        var currentSpeed = physics.LinearVelocity.Length();
        var newVelocity = attackAngle.ToWorldVec() * currentSpeed;
        PhysicsSystem.SetLinearVelocity(projectile, newVelocity, body: physics);
        
        TransformSystem.SetWorldRotation(projectile, attackAngle);
    }

    /// <summary>
    /// Gets the facing direction of an entity.
    /// </summary>
    private Angle GetFacingAngle(EntityUid entity)
    {
        var xform = Transform(entity);
        return TransformSystem.GetWorldRotation(xform);
    }
}
