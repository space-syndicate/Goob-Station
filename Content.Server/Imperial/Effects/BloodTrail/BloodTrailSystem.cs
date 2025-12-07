using Content.Server.Decals;
using Content.Shared.Body.Components;
using Content.Shared.Damage;
using Content.Shared.Decals;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs.Components;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using System.Numerics;

namespace Content.Server.Imperial.BloodTrail
{
    public sealed class BloodTrailSystem : EntitySystem
    {
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly IPrototypeManager _prototype = default!;
        [Dependency] private readonly SharedTransformSystem _transform = default!;
        [Dependency] private readonly IGameTiming _timing = default!;
        [Dependency] private readonly IMapManager _map = default!;
        [Dependency] private readonly DecalSystem _decal = default!;

        private const float MinDistanceBetweenDecals = 0.3f;
        private readonly List<Vector2> _recentDecalPositions = new();
        private TimeSpan _lastCleanupTime;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<BloodTrailComponent, DamageChangedEvent>(OnDamageChanged);
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            if (_timing.CurTime - _lastCleanupTime > TimeSpan.FromSeconds(5))
            {
                _recentDecalPositions.Clear();
                _lastCleanupTime = _timing.CurTime;
            }
        }

        private void OnDamageChanged(EntityUid uid, BloodTrailComponent comp, DamageChangedEvent args)
        {
            if (!comp.Enabled || !args.DamageIncreased || args.DamageDelta == null)
                return;

            if (!CanSpawnBlood(uid, comp))
                return;

            var effectiveDamage = GetEffectiveDamage(args.DamageDelta, comp);
            if (effectiveDamage <= 0)
                return;

            SpawnBloodDecals(uid, effectiveDamage, comp, args.Origin);
            comp.NextSpawnTime = _timing.CurTime + comp.SpawnCooldown;
        }

        private bool CanSpawnBlood(EntityUid uid, BloodTrailComponent comp)
        {
            if (_timing.CurTime < comp.NextSpawnTime)
                return false;

            if (!TryComp(uid, out TransformComponent? xform) || xform.MapID == MapId.Nullspace)
                return false;

            if (TryComp<MobStateComponent>(uid, out var mobState) && mobState.CurrentState == Shared.Mobs.MobState.Dead)
                return false;

            return true;
        }

        private FixedPoint2 GetEffectiveDamage(DamageSpecifier damageSpec, BloodTrailComponent comp)
        {
            FixedPoint2 totalEffectiveDamage = FixedPoint2.Zero;

            foreach (var (damageTypeId, amount) in damageSpec.DamageDict)
            {
                if (amount <= 0 || !IsBleedingDamage(damageTypeId, comp))
                    continue;

                var modifier = comp.DamageTypeModifiers.GetValueOrDefault(damageTypeId, 0.5f);
                var modifiedDamage = amount * modifier;
                totalEffectiveDamage += modifiedDamage;
            }

            return totalEffectiveDamage;
        }

        private bool IsBleedingDamage(string damageType, BloodTrailComponent comp)
        {
            if (comp.DamageTypes.Contains(damageType))
                return true;

            foreach (var group in comp.DamageGroups)
            {
                if (_prototype.TryIndex(group, out var proto) &&
                    proto.DamageTypes.Contains(damageType))
                    return true;
            }

            return false;
        }

        private void SpawnBloodDecals(EntityUid victim, FixedPoint2 effectiveDamage, BloodTrailComponent comp, EntityUid? damageSource)
        {
            if (!TryComp(victim, out TransformComponent? victimXform))
                return;

            var decalCount = GetDecalCount(effectiveDamage);
            decalCount = Math.Min(decalCount, comp.MaxDecals - comp.CurrentDecalCount);

            if (decalCount <= 0)
                return;

            var victimCoords = _transform.GetMapCoordinates(victim);
            var victimWorldPos = victimCoords.Position;

            for (int i = 0; i < decalCount; i++)
            {
                var decalId = _random.Pick(comp.Decals);
                if (!_prototype.HasIndex(decalId))
                    continue;

                var bloodColor = GetBloodColor(victim);

                var (worldPos, rotation) = CalculateDecalPositionAndRotation(victimWorldPos, damageSource, comp.SpreadDistance);

                if (IsTooCloseToRecentDecals(worldPos))
                    continue;

                var mapCoords = new MapCoordinates(worldPos, victimCoords.MapId);

                if (!_map.TryFindGridAt(mapCoords, out var gridUid, out var _))
                    continue;

                var entityCoords = _transform.ToCoordinates(gridUid, mapCoords).Offset(new(-0.5f));

                var decal = new Decal(entityCoords.Position, decalId, bloodColor, rotation, 1, true);
                var success = _decal.TryAddDecal(decal, entityCoords, out _);

                if (success)
                {
                    _recentDecalPositions.Add(worldPos);
                    comp.CurrentDecalCount++;
                }
            }
        }

        private bool IsTooCloseToRecentDecals(Vector2 position)
        {
            foreach (var existingPos in _recentDecalPositions)
            {
                if (Vector2.DistanceSquared(existingPos, position) < MinDistanceBetweenDecals * MinDistanceBetweenDecals)
                    return true;
            }
            return false;
        }

        private Color GetBloodColor(EntityUid victim)
        {
            if (TryComp<BloodstreamComponent>(victim, out var bloodstream) &&
                _prototype.TryIndex(bloodstream.BloodReagent, out var reagent))
                return reagent.SubstanceColor;

            return Color.DarkRed;
        }

        private (Vector2 position, Angle rotation) CalculateDecalPositionAndRotation(Vector2 victimWorldPos, EntityUid? damageSource, float spread)
        {
            Vector2 basePos = victimWorldPos;
            Angle rotation = Angle.FromDegrees(_random.Next(0, 360));

            if (damageSource != null && TryComp(damageSource.Value, out TransformComponent? sourceXform))
            {
                var sourcePos = _transform.GetWorldPosition(sourceXform);
                var attackDirection = (victimWorldPos - sourcePos).Normalized();
                rotation = attackDirection.ToAngle() + Angle.FromDegrees(90);

                var offset = _random.NextFloat(spread * 0.8f, spread * 1.0f);
                basePos = victimWorldPos + attackDirection * offset;
            }
            else
            {
                rotation = Angle.FromDegrees(_random.Next(0, 360));
                var randomDir = new Vector2(_random.NextFloat(-1, 1), _random.NextFloat(-1, 1)).Normalized();
                var offset = _random.NextFloat(spread * 0.8f, spread * 1.0f);
                basePos = victimWorldPos + randomDir * offset;
            }

            var randomOffset = new Vector2(
                _random.NextFloat(-spread * 0.2f, spread * 0.2f),
                _random.NextFloat(-spread * 0.2f, spread * 0.2f)
            );

            return (basePos + randomOffset, rotation);
        }

        private int GetDecalCount(FixedPoint2 effectiveDamage)
        {
            var floatDamage = effectiveDamage.Float();
            return floatDamage switch
            {
                >= 40 => _random.Next(3, 4),
                >= 25 => _random.Next(2, 3),
                >= 12 => _random.Next(1, 2),
                >= 5 => 1,
                _ => 0
            };
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
