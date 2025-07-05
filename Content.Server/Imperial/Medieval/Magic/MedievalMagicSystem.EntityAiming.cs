using System.Linq;
using System.Numerics;
using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.EntityEffects;
using Content.Shared.Ghost;
using Content.Shared.Imperial.CoordsHelper;
using Content.Shared.Imperial.Medieval.Magic;
using Content.Shared.Imperial.TargetOverlay.Events;
using Content.Shared.Item;
using Content.Shared.Physics;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Dynamics;

namespace Content.Server.Imperial.Medieval.Magic;


/// <summary>
/// The server part is responsible for casting entity target overlay spells
/// </summary>
public sealed partial class MedievalMagicSystem
{
    [Dependency] private readonly EntityLookupSystem _entityLookupSystem = default!;

    private void InitializeEntityAimingSpells()
    {
        SubscribeNetworkEvent<TargetOverlayShootEvent>(OnTargetСaptured);
    }

    private void OnTargetСaptured(TargetOverlayShootEvent args)
    {
        if (args.Sender == null) return;

        var performer = GetEntity(args.Performer);
        var sender = GetEntity(args.Sender.Value);

        if (!TryComp<MedievalSpellCasterComponent>(performer, out var spellCasterComponent)) return;
        if (!TryComp<ActionComponent>(sender, out var actionComponent)) return;
        if (actionComponent.Cooldown.HasValue && actionComponent.Cooldown.Value.End >= _timing.CurTime) return;

        if (!spellCasterComponent.TargetStack.TryAdd(sender, args.Targets))
            spellCasterComponent.TargetStack[sender] = spellCasterComponent.TargetStack[sender].Concat(args.Targets).ToList();

        if (!spellCasterComponent.SpellStack.TryGetValue(sender, out var data)) return;

        CastSpell(data);
    }

    protected override void CastHomingProjectilesSpell(MedievalHomingProjectilesSpellData args)
    {
        var action = GetEntity(args.Action);
        var performer = GetEntity(args.Performer);

        _targetOverlaySystem.StopTargeting(performer);

        if (!TryComp<MedievalSpellCasterComponent>(performer, out var spellCasterComponent)) return;
        if (!spellCasterComponent.TargetStack.TryGetValue(action, out var targets))
        {
            spellCasterComponent.SpellStack.TryAdd(action, args);

            return;
        }

        var xform = Transform(performer);

        var spawnMapCoords = _transformSystem.ToMapCoordinates(xform.Coordinates);
        var userVelocity = _physicsSystem.GetMapLinearVelocity(performer);

        for (var i = 0; i < targets.Count; i++)
        {
            var angleIncrease = args.Spread.Degrees / targets.Count;
            var (cursorPosition, target) = targets[i];
            var targetMapCoords = target.HasValue ? _transformSystem.GetMapCoordinates(GetEntity(target.Value)) : cursorPosition;

            var projectilePrototype = args.ProjectilePrototype;
            var evBefore = new MedievalBeforeSpawnEntityBySpellEvent()
            {
                Action = action,
                Performer = performer,
                SpawnedEntityPrototype = projectilePrototype,
                Coordinates = xform.Coordinates
            };
            RaiseLocalEvent(action, ref evBefore);

            if (evBefore.Cancelled) continue;

            var ent = Spawn(evBefore.SpawnedEntityPrototype, xform.Coordinates);
            var rotation = Angle.FromDegrees(args.Spread.Degrees / 2 - angleIncrease * i);
            var direction = targetMapCoords.Position - spawnMapCoords.Position;

            var shootDirection = rotation.RotateVec(
                direction == Vector2.Zero
                    ? xform.LocalRotation.ToVec()
                    : direction
            );

            var ev = new MedievalAfterSpawnEntityBySpellEvent()
            {
                Action = action,
                Performer = performer,
                Rotation = rotation,
                SpawnedEntity = ent
            };

            RaiseLocalEvent(ent, ev);
            RaiseLocalEvent(action, ev);

            if (target.HasValue)
                _homingProjectileSystem.SetTarget(ent, GetEntity(target.Value), args.LinearVelocityIntensy, args.RelativeAngle);
            else
                _homingProjectileSystem.SetTarget(ent, targetMapCoords, args.LinearVelocityIntensy, args.RelativeAngle);

            _gunSystem.ShootProjectile(ent, shootDirection, userVelocity, performer, performer, speed: args.ProjectileSpeed);
        }

        spellCasterComponent.TargetStack.Remove(action);
        spellCasterComponent.SpellStack.Remove(action);

        base.CastHomingProjectilesSpell(args);
    }

    protected override void CastEntityTargetProjectileSpell(MedievalEntityTargetProjectileSpellData args)
    {
        var action = GetEntity(args.Action);
        var performer = GetEntity(args.Performer);

        _targetOverlaySystem.StopTargeting(performer);

        if (!TryComp<MedievalSpellCasterComponent>(performer, out var spellCasterComponent)) return;
        if (!spellCasterComponent.TargetStack.TryGetValue(action, out var targets))
        {
            spellCasterComponent.SpellStack.TryAdd(action, args);

            return;
        }

        var xform = Transform(performer);

        var spawnMapCoords = _transformSystem.ToMapCoordinates(xform.Coordinates);
        var userVelocity = _physicsSystem.GetMapLinearVelocity(performer);

        foreach (var (cursorPosition, target) in targets)
        {
            var targetMapCoords = target.HasValue ? _transformSystem.GetMapCoordinates(GetEntity(target.Value)) : cursorPosition;

            var projectilePrototype = args.ProjectilePrototype;
            var evBefore = new MedievalBeforeSpawnEntityBySpellEvent()
            {
                Action = action,
                Performer = performer,
                SpawnedEntityPrototype = projectilePrototype,
                Coordinates = xform.Coordinates
            };
            RaiseLocalEvent(action, ref evBefore);

            if (evBefore.Cancelled) continue;

            var ent = Spawn(evBefore.SpawnedEntityPrototype, xform.Coordinates);
            var direction = targetMapCoords.Position - spawnMapCoords.Position;

            var ev = new MedievalAfterSpawnEntityBySpellEvent()
            {
                Action = action,
                Performer = performer,
                Rotation = Angle.Zero,
                SpawnedEntity = ent
            };

            RaiseLocalEvent(ent, ev);
            RaiseLocalEvent(action, ev);

            _gunSystem.ShootProjectile(ent, direction, userVelocity, performer, performer, speed: args.ProjectileSpeed);
        }

        spellCasterComponent.TargetStack.Remove(action);
        spellCasterComponent.SpellStack.Remove(action);

        base.CastEntityTargetProjectileSpell(args);
    }

    protected override void CastLightningSpell(MedievalLightningSpellData args)
    {
        var action = GetEntity(args.Action);
        var performer = GetEntity(args.Performer);

        _targetOverlaySystem.StopTargeting(performer);

        if (!TryComp<MedievalSpellCasterComponent>(performer, out var spellCasterComponent)) return;
        if (!spellCasterComponent.TargetStack.TryGetValue(action, out var targets))
        {
            spellCasterComponent.SpellStack.TryAdd(action, args);

            return;
        }

        var performerCoords = _transformSystem.GetWorldPosition(performer);

        for (var i = 0; i < targets.Count; i++)
        {
            var (cursorPosition, netTarget) = targets[i];

            var target = GetEntity(netTarget);
            var direction = cursorPosition.Position - performerCoords;

            var ray = new CollisionRay(performerCoords, direction.Normalized(), (int)CollisionGroup.AllMask);
            var collidedItemEnts = LightningRaycast(cursorPosition.MapId, ray, performer, direction.Length());

            TryChangeTarget(collidedItemEnts, ref target);

            var targetCoords = CoordsHelper.GetCoords(cursorPosition, target, EntityManager);
            args.Seed = args.Seed.HasValue ? args.Seed : args.Seed * i;

            if (target.HasValue)
                args.LightningCollideEffects.ForEach(effect => effect.Effect(new EntityEffectBaseArgs(target.Value, EntityManager)));

            SpawnLightning(performerCoords, targetCoords.Position, args);

            if (args.SpawnedEffectPrototype == null) continue;

            var effectPrototype = args.SpawnedEffectPrototype.Value;
            var evBefore = new MedievalBeforeSpawnEntityBySpellEvent()
            {
                Action = action,
                Performer = performer,
                SpawnedEntityPrototype = effectPrototype,
                Coordinates = targetCoords
            };
            RaiseLocalEvent(action, ref evBefore);

            if (evBefore.Cancelled) continue;

            var ent = Spawn(evBefore.SpawnedEntityPrototype, targetCoords);
            var ev = new MedievalAfterSpawnEntityBySpellEvent()
            {
                Action = action,
                Performer = performer,
                Rotation = Angle.Zero,
                SpawnedEntity = ent
            };

            RaiseLocalEvent(ent, ev);
            RaiseLocalEvent(action, ev);
        }

        spellCasterComponent.TargetStack.Remove(action);
        spellCasterComponent.SpellStack.Remove(action);

        base.CastLightningSpell(args);
    }

    protected override void CastSpawnAimingEntitySpell(MedievalSpawnAimingEntityData args)
    {
        var action = GetEntity(args.Action);
        var performer = GetEntity(args.Performer);

        _targetOverlaySystem.StopTargeting(performer);

        if (!TryComp<MedievalSpellCasterComponent>(performer, out var spellCasterComponent)) return;
        if (!spellCasterComponent.TargetStack.TryGetValue(action, out var targets))
        {
            spellCasterComponent.SpellStack.TryAdd(action, args);

            return;
        }

        var xform = Transform(performer);

        foreach (var (cursorPosition, netTarget) in targets)
        {
            var target = (cursorPosition, GetEntity(netTarget));

            var spawnedEvent = new MedievalBeforeSpawnEntityBySpellEvent()
            {
                Action = action,
                Performer = performer,
                SpawnedEntityPrototype = args.SpawnedEntity,
                Coordinates = xform.Coordinates
            };
            RaiseLocalEvent(action, ref spawnedEvent);

            var entAimingSpawnedEvent = new MedievalBeforeAimingSpawnBySpellEvent()
            {
                Action = action,
                Performer = performer,
                SpawnedEntityPrototype = args.SpawnedEntity,
                Coordinates = spawnedEvent.Coordinates,
                Target = target
            };
            RaiseLocalEvent(action, ref entAimingSpawnedEvent);

            if (spawnedEvent.Cancelled || entAimingSpawnedEvent.Cancelled) continue;

            var ent = Spawn(args.SpawnedEntity, entAimingSpawnedEvent.Coordinates);
            var aimingEvAfter = new MedievalAfterAimingSpawnBySpellEvent()
            {
                Action = action,
                Performer = performer,
                Rotation = Angle.Zero,
                SpawnedEntity = ent,
                Target = target
            };
            var evAfter = new MedievalAfterSpawnEntityBySpellEvent()
            {
                Action = action,
                Performer = performer,
                Rotation = Angle.Zero,
                SpawnedEntity = ent
            };

            RaiseLocalEvent(ent, evAfter);
            RaiseLocalEvent(action, evAfter);

            RaiseLocalEvent(ent, aimingEvAfter);
            RaiseLocalEvent(action, aimingEvAfter);
        }

        spellCasterComponent.TargetStack.Remove(action);
        spellCasterComponent.SpellStack.Remove(action);

        base.CastSpawnAimingEntitySpell(args);
    }


    #region Helpers

    private bool TryChangeTarget(IEnumerable<RayCastResults> rayCastResults, ref EntityUid? target)
    {
        foreach (var rayCastResult in rayCastResults)
        {
            if (HasComp<GhostComponent>(rayCastResult.HitEntity)) continue;
            if (
                HasComp<ItemComponent>(rayCastResult.HitEntity) &&
                TryComp<PhysicsComponent>(rayCastResult.HitEntity, out var physicsComponent)
            )
            {
                if (physicsComponent.BodyStatus != BodyStatus.InAir) continue;

                target = rayCastResult.HitEntity;

                return true;
            }

            target = rayCastResult.HitEntity;

            return true;
        }

        return false;
    }



    private void SpawnLightning(Vector2 uid, Vector2 target, MedievalLightningSpellData data)
    {
        _lightningSystem.SpawnLightningBetween(
            uid,
            target,
            data.LightningColor,
            data.Offset,
            data.LifeTime,
            data.Speed,
            data.Intensity,
            data.Seed,
            data.Amplitude,
            data.Frequency
        );
    }

    private List<RayCastResults> LightningRaycast(MapId mapId, CollisionRay ray, EntityUid ignoredEnt, float maxLength = 50F, bool returnOnFirstHit = true)
    {
        if (!_mapSystem.TryGetMap(mapId, out var mapUid)) return new();
        if (!TryComp<BroadphaseComponent>(mapUid, out var broadphaseComponent)) return new();

        var results = new List<RayCastResults>();
        var endPoint = ray.Position + ray.Direction.Normalized() * maxLength;
        var rayBox = new Box2(Vector2.Min(ray.Position, endPoint), Vector2.Max(ray.Position, endPoint));

        var (_, rot, matrix, invMatrix) = _transformSystem.GetWorldPositionRotationMatrixWithInv(mapUid.Value);

        var position = Vector2.Transform(ray.Position, invMatrix);
        var gridRot = new Angle(-rot.Theta);
        var direction = gridRot.RotateVec(ray.Direction);

        var gridRay = new CollisionRay(position, direction, ray.CollisionMask);

        broadphaseComponent.DynamicTree.QueryRay((in FixtureProxy proxy, in Vector2 point, float distFromOrigin) =>
        {
            if (distFromOrigin > maxLength)
                return true;

            if (proxy.Entity == ignoredEnt)
                return true;

            if (_tagSystem.HasTag(proxy.Entity, "LightningBlacklist"))
                return true;

            var result = new RayCastResults(distFromOrigin, Vector2.Transform(point, matrix), proxy.Entity);
            results.Add(result);

            return true;
        }, gridRay);

        broadphaseComponent.StaticTree.QueryRay((in FixtureProxy proxy, in Vector2 point, float distFromOrigin) =>
        {
            if (distFromOrigin > maxLength)
                return true;

            if (proxy.Entity == ignoredEnt)
                return true;

            if (!_tagSystem.HasTag(proxy.Entity, "Wall"))
                return true;

            if (_tagSystem.HasTag(proxy.Entity, "LightningBlacklist"))
                return true;


            var result = new RayCastResults(distFromOrigin, Vector2.Transform(point, matrix), proxy.Entity);
            results.Add(result);

            return true;
        }, gridRay);

        return returnOnFirstHit && results.Count > 1
            ? new() { results[results.Count - 1] }
            : results;
    }

    #endregion
}
