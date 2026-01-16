using System.Linq;
using System.Numerics;
using Content.Shared.Hands;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Imperial.TargetOverlay;
using Content.Shared.Imperial.TargetOverlay.Events;
using Content.Shared.Imperial.XxRaay.Components;
using Content.Server.Imperial.Medieval.Magic.MedievalHomingProjectile;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Map;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Server.GameObjects;
using Content.Server.Weapons.Ranged.Systems;

namespace Content.Server.Imperial.XxRaay.Systems;

/// <summary>
/// Система для управления плечевой ракетной установкой с системой захвата целей.
/// </summary>
public sealed class ShoulderRocketLauncherSystem : EntitySystem
{
    [Dependency] private readonly SharedTargetOverlaySystem _targetOverlaySystem = default!;
    [Dependency] private readonly MedievalHomingProjectileSystem _homingProjectileSystem = default!;
    [Dependency] private readonly GunSystem _gunSystem = default!;
    [Dependency] private readonly SharedTransformSystem _transformSystem = default!;
    [Dependency] private readonly PhysicsSystem _physicsSystem = default!;
    [Dependency] private readonly RechargeShoulderRocketLauncherSystem _rechargeSystem = default!;
    [Dependency] private readonly SharedHandsSystem _handsSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ShoulderRocketLauncherComponent, HandSelectedEvent>(OnHandSelected);
        SubscribeLocalEvent<ShoulderRocketLauncherComponent, HandDeselectedEvent>(OnHandDeselected);
        SubscribeNetworkEvent<TargetOverlayShootEvent>(OnTargetOverlayShoot);
    }

    private void OnHandSelected(Entity<ShoulderRocketLauncherComponent> entity, ref HandSelectedEvent args)
    {
        var component = entity.Comp;
        var user = args.User;

        if (component.Charges > 0 && !component.IsTargeting)
        {
            component.IsTargeting = true;
            Dirty(entity);
            _targetOverlaySystem.StartTargeting(user, entity, maxTargetCount: 6, 
                whiteListComponents: new HashSet<string> { "Transform" });
        }
    }

    private void OnHandDeselected(Entity<ShoulderRocketLauncherComponent> entity, ref HandDeselectedEvent args)
    {
        var component = entity.Comp;
        var user = args.User;

        if (component.IsTargeting)
        {
            component.IsTargeting = false;
            Dirty(entity);
            _targetOverlaySystem.StopTargeting(user);
        }
    }

    private void OnTargetOverlayShoot(TargetOverlayShootEvent args)
    {
        if (args.Sender == null)
            return;

        var sender = GetEntity(args.Sender.Value);
        var performer = GetEntity(args.Performer);

        if (!TryComp<ShoulderRocketLauncherComponent>(sender, out var component))
            return;

        if (!_handsSystem.IsHolding(performer, sender, out _))
            return;

        if (component.Charges <= 0)
            return;

        var targetsToShoot = args.Targets.Take(component.Charges).ToList();
        
        if (targetsToShoot.Count == 0)
            return;

        var xform = Transform(performer);
        var spawnMapCoords = _transformSystem.ToMapCoordinates(xform.Coordinates);
        var userVelocity = _physicsSystem.GetMapLinearVelocity(performer);

        foreach (var (cursorPosition, netTarget) in targetsToShoot)
        {
            EntityUid? targetEntity = null;
            if (netTarget.HasValue)
                targetEntity = GetEntity(netTarget.Value);
            
            var targetMapCoords = targetEntity != null 
                ? _transformSystem.GetMapCoordinates(targetEntity.Value) 
                : cursorPosition;

            var rocket = Spawn(component.ProjectilePrototype, xform.Coordinates);
            
            if (targetEntity != null)
            {
                _homingProjectileSystem.SetTarget(rocket, targetEntity.Value, linearVelocityIntensy: 1.0f, Angle.Zero, rotateToTarget: true);
            }
            else
            {
                _homingProjectileSystem.SetTarget(rocket, targetMapCoords, linearVelocityIntensy: 1.0f, Angle.Zero, rotateToTarget: true);
            }

            var direction = targetMapCoords.Position - spawnMapCoords.Position;
            if (direction == Vector2.Zero)
                direction = _transformSystem.GetWorldRotation(performer).ToVec();

            _gunSystem.ShootProjectile(rocket, direction, userVelocity, performer, performer, speed: 15f);
        }

        component.Charges -= targetsToShoot.Count;
        Dirty(sender, component);

        if (TryComp<RechargeShoulderRocketLauncherComponent>(sender, out var recharge))
        {
            _rechargeSystem.Reset(sender, recharge);
        }
    }
}

