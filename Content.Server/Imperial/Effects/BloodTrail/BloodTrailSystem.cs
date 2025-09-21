using Content.Shared.Damage;
using Robust.Shared.Map;
using Robust.Shared.Random;
using Robust.Shared.Prototypes;
using Content.Shared.Damage.Prototypes;
using Content.Server.Decals;
using Content.Shared.FixedPoint;
using Robust.Server.GameObjects;
using System.Numerics;
using Robust.Shared.Timing;
using Content.Shared.Mobs.Components;
using Content.Shared.GameTicking;

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

            foreach (var (damageType, amount) in args.DamageDelta.DamageDict)
            {
                var causesBleeding = ShouldCauseBleeding(damageType, component);

                if (amount > 0 && causesBleeding)
                {
                    hasBleedingDamage = true;
                    break;
                }
            }

            if (!hasBleedingDamage)
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

            Angle damageDirection;
            if (args.Origin != null && TryComp(args.Origin.Value, out TransformComponent? originXform))
            {
                var worldPos = _transformSystem.GetWorldPosition(xform);
                var originWorldPos = _transformSystem.GetWorldPosition(originXform);
                var damageVector = originWorldPos - worldPos;

                if (damageVector.LengthSquared() > 0.01f)
                {
                    damageDirection = damageVector.ToWorldAngle();
                }
                else
                {
                    damageDirection = Angle.FromDegrees(_random.Next(0, 360));
                }
            }
            else
            {
                damageDirection = Angle.FromDegrees(_random.Next(0, 360));
            }

            SpawnBloodDecals(uid, mechanicalDamage, component, damageDirection, xform);

            component.NextSpawnTime = _gameTiming.CurTime + component.SpawnCooldown;
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

        private void SpawnBloodDecals(EntityUid uid, FixedPoint2 damage, BloodTrailComponent component, Angle damageDirection, TransformComponent xform)
        {

            if (damage < component.MinDamageToSpawn)
            {
                return;
            }

            var decalCount = CalculateDecalCount(damage);
            var spreadDistance = CalculateSpreadDistance(damage);

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

            var mapId = xform.MapID;
            if (!_mapSystem.MapExists(mapId))
            {
                return;
            }

            var worldPos = _transformSystem.GetWorldPosition(xform);

            int spawnedCount = 0;
            for (int i = 0; i < decalCount; i++)
            {
                var decalType = GetRandomDecal(component);
                var offsetPos = CalculateOffsetPosition(worldPos, spreadDistance, i, damageDirection);

                if (SpawnDecal(decalType, offsetPos, mapId, component.BloodColor, damageDirection, tilePos))
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

        private Vector2 CalculateOffsetPosition(Vector2 worldPosition, float spreadDistance, int index, Angle damageDirection)
        {
            var direction = new Vector2(
                (float)Math.Cos(damageDirection.Theta),
                (float)Math.Sin(damageDirection.Theta)
            );

            var offsetPos = worldPosition + direction * (spreadDistance * (index + 1));

            var perpendicular = new Vector2(-direction.Y, direction.X);
            offsetPos += perpendicular * _random.NextFloat(-0.5f, 0.5f);

            offsetPos += direction * _random.NextFloat(-0.2f, 0.2f);

            return offsetPos;
        }

        private int CalculateDecalCount(FixedPoint2 damage)
        {
            var floatDamage = damage.Float();
            var count = floatDamage switch
            {
                >= 50 => _random.Next(3, 6),
                >= 35 => _random.Next(2, 4),
                >= 15 => _random.Next(1, 3),
                >= 5 => _random.Next(1, 2),
                _ => 0
            };
            return count;
        }

        private float CalculateSpreadDistance(FixedPoint2 damage)
        {
            var floatDamage = damage.Float();
            var distance = floatDamage switch
            {
                >= 50 => _random.NextFloat(0.4f, 0.8f),
                >= 35 => _random.NextFloat(0.3f, 0.6f),
                >= 15 => _random.NextFloat(0.2f, 0.4f),
                >= 5 => _random.NextFloat(0.1f, 0.3f),
                _ => 0f
            };
            return distance;
        }

        private bool SpawnDecal(string decalId, Vector2 position, MapId mapId, Color bloodColor, Angle damageDirection, Vector2i tilePos)
        {

            if (!_mapSystem.MapExists(mapId))
            {
                return false;
            }

            var mapUid = _mapSystem.GetMap(mapId);
            if (mapUid == EntityUid.Invalid)
            {
                return false;
            }

            var rotationVariation = _random.NextFloat(-MathF.PI / 6f, MathF.PI / 6f);
            var finalRotation = damageDirection + rotationVariation;

            var coordinates = new EntityCoordinates(mapUid, position);

            var success = _decalSystem.TryAddDecal(
                decalId,
                coordinates,
                out _,
                bloodColor,
                finalRotation,
                cleanable: true
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
