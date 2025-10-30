using System.Numerics;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Physics;
using Content.Shared.Projectiles;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Weapons.Reflect;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Timing;

namespace Content.Shared.Imperial.XxRaay.Zero.KatanaDeflect;

/// <summary>
/// System that handles katana projectile deflection mechanics.
/// When a katana with KatanaDeflectComponent attacks, it temporarily enables ReflectComponent
/// to deflect projectiles in the attack direction.
/// </summary>
public class KatanaDeflectSystem : EntitySystem
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
            if (!comp.LastAttackTime.HasValue) continue;
            if (currentTime > comp.LastAttackTime + TimeSpan.FromSeconds(comp.ActiveWindow))
            {
                comp.LastAttackTime = null;
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
        component.LastAttackDirection = GetFacingDirection(entity.Owner);
        component.HasDeflected = false;
        
        if (TryComp<ReflectComponent>(entity, out var reflect))
        {
            reflect.ReflectProb = 1.0f;
            Dirty(entity, reflect);
        }
        
        Dirty(entity, component);
    }

    private bool TryGetKatanaWithDeflect(EntityUid user, out Entity<KatanaDeflectComponent> katana)
    {
        katana = default;
        
        if (TryComp<HandsComponent>(user, out var hands))
        {
            foreach (var handId in hands.SortedHands)
            {
                if (HandsSystem.TryGetHeldItem((user, hands), handId, out var held) && held.HasValue)
                {
                    if (TryComp<KatanaDeflectComponent>(held.Value, out _))
                    {
                        katana = (held.Value, Comp<KatanaDeflectComponent>(held.Value));
                        return true;
                    }
                }
            }
        }
        
        return false;
    }

    private void OnProjectileReflectAttempt(Entity<KatanaDeflectComponent> entity, ref ProjectileReflectAttemptEvent ev)
    {
        if (ev.Cancelled) return;
        
        var component = entity.Comp;
        
        if (!component.LastAttackTime.HasValue) return;
        if (component.HasDeflected) return;
        if (GameTiming.CurTime > component.LastAttackTime + TimeSpan.FromSeconds(component.ActiveWindow))
            return;
        
        if (!IsProjectileInDeflectionCone(ev.ProjUid, entity.Owner, component))
            return;
        
        if (component.LastAttackDirection.HasValue)
        {
            OverrideReflectionDirection(ev.ProjUid, component.LastAttackDirection.Value);
        }
        
        component.HasDeflected = true;
        component.LastAttackTime = null;
        Dirty(entity, component);
    }

    private bool IsProjectileInDeflectionCone(EntityUid projectile, EntityUid wielder, KatanaDeflectComponent comp)
    {
        var projPos = TransformSystem.GetWorldPosition(projectile);
        var wielderPos = TransformSystem.GetWorldPosition(wielder);
        var toProjectile = projPos - wielderPos;
        
        var angle = Angle.FromWorldVec(toProjectile);
        var attackAngle = Angle.FromWorldVec(comp.LastAttackDirection ?? Vector2.Zero);
        var angleDiff = (angle - attackAngle).Reduced().FlipPositive();
        
        return !(angleDiff > comp.DeflectAngle / 2 && Math.Tau - angleDiff > comp.DeflectAngle / 2);
    }

    private void OverrideReflectionDirection(EntityUid projectile, Vector2 attackDir)
    {
        if (!TryComp<PhysicsComponent>(projectile, out var physics))
            return;
            
        var currentSpeed = physics.LinearVelocity.Length();
        var newVelocity = attackDir.Normalized() * currentSpeed;
        PhysicsSystem.SetLinearVelocity(projectile, newVelocity, body: physics);
        
        var newRotation = attackDir.ToAngle();
        TransformSystem.SetWorldRotation(projectile, newRotation);
    }

    /// <summary>
    /// Gets the facing direction of an entity.
    /// </summary>
    private Vector2 GetFacingDirection(EntityUid entity)
    {
        var xform = Transform(entity);
        return TransformSystem.GetWorldRotation(xform).ToWorldVec();
    }
}
