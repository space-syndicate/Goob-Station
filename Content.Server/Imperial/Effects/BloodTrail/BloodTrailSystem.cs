using Robust.Shared.Map;
using Robust.Shared.Random;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Content.Shared.Mobs.Components;
using Content.Shared.GameTicking;
using Content.Shared.Decals;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.FixedPoint;
using Content.Server.Decals;
using System.Numerics;

namespace Content.Server.Imperial.BloodTrail
{
    public sealed class BloodTrailSystem : EntitySystem
    {
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly SharedTransformSystem _transformSystem = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly DecalSystem _decalSystem = default!;
        [Dependency] private readonly SharedMapSystem _mapSystem = default!;

        private const int MaxBloodDecalsPerTile = 5;
        private readonly Dictionary<string, bool> _damageGroupCache = new();
        private readonly Dictionary<Vector2i, int> _bloodDecalCountPerTile = new();

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<BloodTrailComponent, DamageChangedEvent>(OnDamageChanged);
            SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);
        }

        private void OnRoundRestart(RoundRestartCleanupEvent ev)
        {
            _bloodDecalCountPerTile.Clear();
        }

        private void OnDamageChanged(EntityUid uid, BloodTrailComponent component, DamageChangedEvent args)
        {
            if (!component.Enabled)
                return;

            if (!args.DamageIncreased || args.DamageDelta == null)
                return;

            bool hasBleedingDamage = false;
            float highestChance = 0f;

            foreach (var (damageType, amount) in args.DamageDelta.DamageDict)
            {
                if (amount > 0 && ShouldCauseBleeding(damageType, component))
                {
                    hasBleedingDamage = true;
                    var chance = GetDamageTypeChance(damageType, component);
                    if (chance > highestChance)
                        highestChance = chance;
                }
            }

            if (!hasBleedingDamage || _random.NextFloat() > highestChance)
                return;

            if (_gameTiming.CurTime < component.NextSpawnTime)
                return;

            if (!TryComp(uid, out TransformComponent? xform) || xform.MapID == MapId.Nullspace)
                return;

            if (TryComp<MobStateComponent>(uid, out var mobState) && mobState.CurrentState == Shared.Mobs.MobState.Dead)
                return;

            var mechanicalDamage = GetMechanicalDamage(args.DamageDelta, component);
            if (mechanicalDamage <= 0)
                return;

            SpawnBloodDecals(uid, mechanicalDamage, component, xform, args.Origin);
            component.NextSpawnTime = _gameTiming.CurTime + component.SpawnCooldown;
        }

        private float GetDamageTypeChance(string damageTypeId, BloodTrailComponent component)
        {
            return component.DamageTypeChances.TryGetValue(damageTypeId, out var chance) ? chance : 0.7f;
        }

        private FixedPoint2 GetMechanicalDamage(DamageSpecifier damageSpec, BloodTrailComponent component)
        {
            FixedPoint2 totalDamage = FixedPoint2.Zero;
            foreach (var (damageTypeId, amount) in damageSpec.DamageDict)
            {
                if (amount > 0 && ShouldCauseBleeding(damageTypeId, component))
                    totalDamage += amount;
            }
            return totalDamage;
        }

        private bool ShouldCauseBleeding(string damageTypeId, BloodTrailComponent component)
        {
            if (component.DamageTypes.Contains(damageTypeId))
                return true;

            foreach (var groupId in component.DamageGroups)
            {
                if (!_damageGroupCache.TryGetValue(groupId, out var causesBleeding))
                {
                    if (_prototypeManager.TryIndex<DamageGroupPrototype>(groupId, out var groupProto))
                    {
                        causesBleeding = groupProto.DamageTypes.Contains(damageTypeId);
                        _damageGroupCache[groupId] = causesBleeding;
                    }
                    else
                    {
                        _damageGroupCache[groupId] = false;
                        continue;
                    }
                }

                if (causesBleeding)
                    return true;
            }

            return false;
        }

        private void SpawnBloodDecals(EntityUid uid, FixedPoint2 damage, BloodTrailComponent component, TransformComponent xform, EntityUid? damageSource)
        {
            if (damage < component.MinDamageToSpawn)
                return;

            var decalCount = CalculateDecalCount(damage);
            var victimWorldPos = _transformSystem.GetWorldPosition(xform);
            var mapCoords = new MapCoordinates(victimWorldPos, xform.MapID);

            var tilePos = new Vector2i(
                (int)MathF.Floor(mapCoords.X),
                (int)MathF.Floor(mapCoords.Y)
            );

            if (_bloodDecalCountPerTile.TryGetValue(tilePos, out var currentCount) && currentCount >= MaxBloodDecalsPerTile)
                return;

            decalCount = Math.Min(decalCount, component.MaxDecals - component.CurrentDecalCount);
            if (decalCount <= 0)
                return;

            for (int i = 0; i < decalCount; i++)
            {
                var decalType = GetRandomDecal(component);

                if (SpawnDecal(decalType, mapCoords, component.BloodColor, tilePos, uid, damageSource, component))
                {
                    component.CurrentDecalCount++;
                }
            }
        }

        private bool SpawnDecal(string decalId, MapCoordinates mapCoords, Color bloodColor, Vector2i tilePos, EntityUid victimUid, EntityUid? damageSource, BloodTrailComponent component)
        {
            if (!_prototypeManager.TryIndex<DecalPrototype>(decalId, out _))
                return false;

            if (!_mapManager.TryFindGridAt(mapCoords, out var gridUid, out _))
                return false;

            var finalPosition = CalculateDecalPosition(victimUid, damageSource, mapCoords, component.SpreadDistance);
            var mapCoordsFinal = new MapCoordinates(finalPosition, mapCoords.MapId);
            var entityCoords = _transformSystem.ToCoordinates(gridUid, mapCoordsFinal);

            Angle rotation = Angle.Zero;

            if (damageSource != null && Exists(damageSource.Value) &&
                TryComp(damageSource.Value, out TransformComponent? sourceXform) &&
                TryComp(victimUid, out TransformComponent? victimXform))
            {
                var sourcePos = _transformSystem.GetWorldPosition(sourceXform);
                var victimPos = _transformSystem.GetWorldPosition(victimXform);

                var direction = victimPos - sourcePos;

                if (direction.LengthSquared() > 0.1f)
                {
                    var hitAngle = Angle.FromWorldVec(direction.Normalized());
                    rotation = hitAngle + MathF.PI;
                }
            }

            var decal = new Decal(
                coordinates: entityCoords.Position,
                id: decalId,
                color: bloodColor,
                angle: rotation,
                zIndex: 1,
                cleanable: true
            );

            var success = _decalSystem.TryAddDecal(decal, entityCoords, out _);

            if (success)
            {
                _bloodDecalCountPerTile[tilePos] = _bloodDecalCountPerTile.GetValueOrDefault(tilePos) + 1;
            }

            return success;
        }

        private Vector2 CalculateDecalPosition(EntityUid victimUid, EntityUid? damageSource, MapCoordinates victimCoords, float spreadDistance)
        {
            if (damageSource == null || !Exists(damageSource.Value) ||
                !TryComp(damageSource.Value, out TransformComponent? sourceXform) ||
                !TryComp(victimUid, out TransformComponent? victimXform))
            {
                return victimCoords.Position + new Vector2(
                    _random.NextFloat(-spreadDistance, spreadDistance),
                    _random.NextFloat(-spreadDistance, spreadDistance)
                );
            }

            var sourcePos = _transformSystem.GetWorldPosition(sourceXform);
            var victimPos = _transformSystem.GetWorldPosition(victimXform);

            var direction = victimPos - sourcePos;

            if (direction.LengthSquared() > 0.1f)
            {
                var normalizedDirection = direction.Normalized();

                var offsetDistance = _random.NextFloat(spreadDistance * 0.5f, spreadDistance * 1.5f);

                return victimPos + normalizedDirection * offsetDistance;
            }

            return victimCoords.Position;
        }
        private string GetRandomDecal(BloodTrailComponent component)
        {
            return _random.Pick(component.Decals);
        }

        private int CalculateDecalCount(FixedPoint2 damage)
        {
            var floatDamage = damage.Float();
            return floatDamage switch
            {
                >= 50 => _random.Next(2, 4),
                >= 30 => _random.Next(1, 3),
                >= 15 => _random.Next(1, 2),
                >= 5 => 1,
                _ => 0
            };
        }

        public void CleanupTile(Vector2i tilePos)
        {
            _bloodDecalCountPerTile[tilePos] = 0;
        }

        public void ResetSpawnCooldown(BloodTrailComponent component)
        {
            component.NextSpawnTime = TimeSpan.Zero;
        }

        public void ResetDecalCount(BloodTrailComponent component)
        {
            component.CurrentDecalCount = 0;
        }
    }
}
