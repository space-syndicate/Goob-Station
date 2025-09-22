using Robust.Shared.Map;
using Robust.Shared.Random;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Physics.Components;
using Robust.Server.GameObjects;
using Content.Shared.Mobs.Components;
using Content.Shared.GameTicking;
using Content.Shared.Decals;
using Content.Shared.Projectiles;
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
        [Dependency] private readonly MapSystem _mapSystem = default!;
        [Dependency] private readonly DecalSystem _decalSystem = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly IMapManager _mapManager = default!;

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
            if (!args.DamageIncreased || args.DamageDelta == null)
            {
                return;
            }

            bool hasBleedingDamage = false;
            float highestChance = 0f;

            foreach (var (damageType, amount) in args.DamageDelta.DamageDict)
            {
                var causesBleeding = ShouldCauseBleeding(damageType, component);

                if (amount > 0 && causesBleeding)
                {
                    hasBleedingDamage = true;
                    var chance = GetDamageTypeChance(damageType, component);
                    if (chance > highestChance)
                    {
                        highestChance = chance;
                    }
                }
            }

            if (!hasBleedingDamage)
            {
                return;
            }

            if (_random.NextFloat() > highestChance)
            {
                return;
            }

            if (_gameTiming.CurTime < component.NextSpawnTime)
            {
                return;
            }

            if (!TryComp(uid, out TransformComponent? xform) || xform.MapID == MapId.Nullspace)
            {
                return;
            }

            if (TryComp<MobStateComponent>(uid, out var mobState) && mobState.CurrentState == Shared.Mobs.MobState.Dead)
            {
                return;
            }

            var mechanicalDamage = GetMechanicalDamage(args.DamageDelta, component);

            if (mechanicalDamage <= 0)
            {
                return;
            }

            Angle bloodDirection = CalculateBloodDirection(uid, xform, args.Origin);

            SpawnBloodDecals(uid, mechanicalDamage, component, bloodDirection, xform, args.Origin);

            component.NextSpawnTime = _gameTiming.CurTime + component.SpawnCooldown;
        }

        private float GetDamageTypeChance(string damageTypeId, BloodTrailComponent component)
        {
            if (component.DamageTypeChances.TryGetValue(damageTypeId, out var chance))
            {
                return chance;
            }

            return 0.7f;
        }

        private Angle CalculateBloodDirection(EntityUid targetUid, TransformComponent targetXform, EntityUid? damageSource)
        {
            if (damageSource == null || !Exists(damageSource))
            {
                return Angle.FromDegrees(_random.Next(0, 360));
            }

            var sourceUid = damageSource.Value;
            var targetMapPos = _transformSystem.GetMapCoordinates(targetUid, xform: targetXform);

            if (HasComp<ProjectileComponent>(sourceUid) && TryComp<PhysicsComponent>(sourceUid, out var physics))
            {
                if (physics.LinearVelocity.LengthSquared() > 0.1f)
                {
                    return physics.LinearVelocity.ToWorldAngle() + MathF.PI;
                }
            }

            if (TryComp(sourceUid, out TransformComponent? sourceXform))
            {
                var sourceMapPos = _transformSystem.GetMapCoordinates(sourceUid, xform: sourceXform);
                var directionVector = targetMapPos.Position - sourceMapPos.Position;

                if (directionVector.LengthSquared() > 0.01f)
                {
                    return directionVector.ToWorldAngle() + MathF.PI;
                }
            }
            return Angle.FromDegrees(_random.Next(0, 360));
        }

        private FixedPoint2 GetMechanicalDamage(DamageSpecifier damageSpec, BloodTrailComponent component)
        {
            FixedPoint2 totalDamage = FixedPoint2.Zero;

            foreach (var (damageTypeId, amount) in damageSpec.DamageDict)
            {
                if (amount <= 0)
                    continue;

                if (ShouldCauseBleeding(damageTypeId, component))
                {
                    totalDamage += amount;
                }
            }

            return totalDamage;
        }

        private bool ShouldCauseBleeding(string damageTypeId, BloodTrailComponent component)
        {
            if (component.DamageTypes.Contains(damageTypeId))
            {
                return true;
            }

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
                {
                    return true;
                }
            }

            return false;
        }

        private void SpawnBloodDecals(EntityUid uid, FixedPoint2 damage, BloodTrailComponent component, Angle bloodDirection, TransformComponent xform, EntityUid? damageSource)
        {
            if (damage < component.MinDamageToSpawn)
            {
                return;
            }

            var decalCount = CalculateDecalCount(damage);
            var baseSpreadDistance = CalculateBaseSpreadDistance(damage, damageSource);

            var mapCoordinates = _transformSystem.GetMapCoordinates(uid, xform: xform);

            if (!_mapManager.TryFindGridAt(mapCoordinates, out var gridUid, out var grid))
            {
                return;
            }

            var tileRef = _mapSystem.GetTileRef(gridUid, grid, xform.Coordinates);
            var tilePos = tileRef.GridIndices;

            if (_bloodDecalCountPerTile.TryGetValue(tilePos, out var currentCount) && currentCount >= MaxBloodDecalsPerTile)
            {
                return;
            }

            decalCount = Math.Min(decalCount, component.MaxDecals - component.CurrentDecalCount);

            if (decalCount <= 0)
            {
                return;
            }

            var worldPos = _transformSystem.GetWorldPosition(xform);

            int spawnedCount = 0;
            for (int i = 0; i < decalCount; i++)
            {
                var decalType = GetRandomDecal(component);

                var offsetPos = CalculateOffsetPosition(worldPos, baseSpreadDistance, i, Math.Max(1, decalCount), bloodDirection, damageSource);

                if (SpawnDecal(decalType, offsetPos, xform.MapID, component.BloodColor, bloodDirection, tilePos))
                {
                    component.CurrentDecalCount++;
                    spawnedCount++;
                }
            }
        }

        private string GetRandomDecal(BloodTrailComponent component)
        {
            var decal = _random.Pick(component.Decals);
            return decal;
        }

        private Vector2 CalculateOffsetPosition(Vector2 worldPosition, float baseSpreadDistance, int index, int totalDecals, Angle bloodDirection, EntityUid? damageSource)
        {
            var direction = new Vector2((float)Math.Cos(bloodDirection.Theta), (float)Math.Sin(bloodDirection.Theta));

            var distanceMultiplier = (index + 1f) / totalDecals;
            var mainOffset = direction * baseSpreadDistance * distanceMultiplier * 1.5f;

            var perpendicular = new Vector2(-direction.Y, direction.X);
            var perpendicularOffset = perpendicular * _random.NextFloat(-0.2f, 0.2f);

            var randomForwardOffset = direction * _random.NextFloat(-0.05f, 0.1f);

            return worldPosition + mainOffset + perpendicularOffset + randomForwardOffset;
        }

        private int CalculateDecalCount(FixedPoint2 damage)
        {
            var floatDamage = damage.Float();
            var count = floatDamage switch
            {
                >= 50 => _random.Next(2, 4),
                >= 30 => _random.Next(2, 3),
                >= 15 => _random.Next(1, 2),
                >= 5 => _random.Next(1, 2),
                _ => 0
            };
            return count;
        }

        private float CalculateBaseSpreadDistance(FixedPoint2 damage, EntityUid? damageSource)
        {
            var floatDamage = damage.Float();
            var distance = floatDamage switch
            {
                >= 50 => _random.NextFloat(0.8f, 1.5f),
                >= 30 => _random.NextFloat(0.6f, 1.0f),
                >= 15 => _random.NextFloat(0.4f, 0.7f),
                >= 5 => _random.NextFloat(0.2f, 0.4f),
                _ => 0.1f
            };

            if (damageSource.HasValue && HasComp<ProjectileComponent>(damageSource.Value))
            {
                distance *= 1.5f;
            }

            return distance;
        }

        private bool SpawnDecal(string decalId, Vector2 position, MapId mapId, Color bloodColor, Angle bloodDirection, Vector2i tilePos)
        {
            var mapCoords = new MapCoordinates(position, mapId);
            if (!_mapManager.TryFindGridAt(mapCoords, out var gridUid, out var grid))
            {
                return false;
            }

            if (!_prototypeManager.TryIndex<DecalPrototype>(decalId, out _))
            {
                return false;
            }

            var localPos = _mapSystem.WorldToLocal(gridUid, grid, mapCoords.Position);
            var coordinates = new EntityCoordinates(gridUid, localPos);

            var rotationVariation = _random.NextFloat(-MathF.PI / 6f, MathF.PI / 6f);
            var finalRotation = bloodDirection + rotationVariation;

            var decal = new Decal(
                coordinates.Position,
                decalId,
                bloodColor,
                finalRotation,
                zIndex: 1,
                cleanable: true
            );

            var success = _decalSystem.TryAddDecal(
                decal,
                coordinates,
                out _
            );

            if (success)
            {
                if (_bloodDecalCountPerTile.ContainsKey(tilePos))
                {
                    _bloodDecalCountPerTile[tilePos]++;
                }
                else
                {
                    _bloodDecalCountPerTile[tilePos] = 1;
                }
            }

            return success;
        }

        public void CleanupTile(Vector2i tilePos)
        {
            if (_bloodDecalCountPerTile.ContainsKey(tilePos))
            {
                _bloodDecalCountPerTile[tilePos] = 0;
            }
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
