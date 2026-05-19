using Content.Shared.Mobs.Components;
using Content.Shared.Mobs;
using Robust.Shared.Map;
using Robust.Shared.Timing;
using Robust.Shared.Audio.Systems;
using Content.Shared.Jittering;
using Content.Shared.Interaction.Events;
using Content.Shared.Movement.Events;
using Content.Shared.ActionBlocker;

namespace Content.Shared.Imperial.Mobs.Phantomor
{
    public sealed class PhantomorTentacleSystem : EntitySystem
    {
        [Dependency] private readonly SharedTransformSystem _transform = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly SharedAudioSystem _audio = default!;
        [Dependency] private readonly SharedJitteringSystem _jitterSystem = default!;
        [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;
        [Dependency] private readonly EntityLookupSystem _lookup = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<PhantomorSummonTentacleAction>(OnTentacleAction);

            SubscribeLocalEvent<PhantomorMovementBlockComponent, AttackAttemptEvent>(OnAttackAttemptBlocked);
            SubscribeLocalEvent<PhantomorMovementBlockComponent, UpdateCanMoveEvent>(OnUpdateCanMove);
        }

        private void OnAttackAttemptBlocked(EntityUid uid, PhantomorMovementBlockComponent comp, ref AttackAttemptEvent args)
        {
            if (!Exists(uid))
                return;

            // блокировка атаки
            if (comp.AttackBlocked && _gameTiming.CurTime < comp.AttackBlockedUntil)
            {
                args.Cancel();
                return;
            }
            // разблокировка атаки
            if (_gameTiming.CurTime >= comp.AttackBlockedUntil && comp.AttackBlocked)
            {
                comp.AttackBlocked = false;
                Dirty(uid, comp);
            }
        }
        private void OnUpdateCanMove(EntityUid uid, PhantomorMovementBlockComponent comp, ref UpdateCanMoveEvent args)
        {
            if (!Exists(uid))
                return;

            // блокировка движения
            if (comp.WalkBlocked && _gameTiming.CurTime < comp.WalkBlockedUntil)
            {
                args.Cancel();
            }
            // разблокировка движения
            else if (comp.WalkBlocked && _gameTiming.CurTime >= comp.WalkBlockedUntil)
            {
                comp.WalkBlocked = false;
                Dirty(uid, comp);
            }
        }

        private void OnTentacleAction(PhantomorSummonTentacleAction args)
        {
            if (args.Handled)
                return;

            var entity = args.Performer;

            // кд между телепортами
            if (args.LastTeleport.TryGetValue(entity, out var lastTelep) && _gameTiming.CurTime - lastTelep < args.TeleportCooldown)
                return;

            if (AttemptTeleportPhantomor(entity, args))
            {
                args.LastTeleport[entity] = _gameTiming.CurTime;
                args.Handled = true;
            }
        }

        private bool AttemptTeleportPhantomor(EntityUid entityTeleport, PhantomorSummonTentacleAction args)
        {
            if (!Exists(entityTeleport))
                return false;

            var playerTransform = Transform(entityTeleport);
            var playerPosition = playerTransform.WorldPosition;
            var mapId = playerTransform.MapID;
            var mapCoordinates = new MapCoordinates(playerPosition, mapId);
            var nearbyEntities = _lookup.GetEntitiesInRange(mapCoordinates, 10f);

            EntityUid? targetEntity = null;
            var nearestDistance = float.MaxValue;

            foreach (var uid in nearbyEntities)
            {
                if (!Exists(uid) || uid == entityTeleport)
                    continue;

                if (!TryComp<MobStateComponent>(uid, out var mobState))
                    continue;

                if (mobState.CurrentState != MobState.Alive)
                    continue;

                var transform = Transform(uid);
                var entityPos = transform.WorldPosition;
                var distance = (entityPos - playerPosition).Length();
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    targetEntity = uid;
                }
            }

            if (targetEntity == null)
                return false;

            // телепорт за спину цели
            var targetTransform = Transform(targetEntity.Value);
            var behindDirection = targetTransform.LocalRotation.ToWorldVec().Normalized();
            var behindPosition = targetTransform.Coordinates.Offset(-behindDirection);
            _transform.SetCoordinates(entityTeleport, behindPosition);

            // временная блокировка движения и атаки
            var blockComp = EnsureComp<PhantomorMovementBlockComponent>(entityTeleport);
            blockComp.WalkBlocked = true;
            blockComp.WalkBlockedUntil = _gameTiming.CurTime + args.FreezeWalking;

            blockComp.AttackBlocked = true;
            blockComp.AttackBlockedUntil = _gameTiming.CurTime + args.FreezeAttack;

            _actionBlocker.UpdateCanMove(entityTeleport);

            _audio.PlayPvs(args.TeleportSound, entityTeleport);
            _jitterSystem.DoJitter(entityTeleport, args.ShakingTime, refresh: true, amplitude: 20f, frequency: 6f);

            var directionToTarget = (_transform.GetWorldPosition(targetEntity.Value) - _transform.GetWorldPosition(entityTeleport)).Normalized();
            var angleToTarget = Math.Atan2(directionToTarget.Y, directionToTarget.X);
            _transform.SetWorldRotation(entityTeleport, angleToTarget);

            return true;
        }
        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            var query = EntityQueryEnumerator<PhantomorMovementBlockComponent>();
            while (query.MoveNext(out var uid, out var comp))
            {
                var needsUpdate = false;

                // проверка разблокировки движения
                if (comp.WalkBlocked && _gameTiming.CurTime >= comp.WalkBlockedUntil)
                {
                    comp.WalkBlocked = false;
                    needsUpdate = true;
                    Dirty(uid, comp);
                }

                // обновляем статус
                if (needsUpdate)
                {
                    _actionBlocker.UpdateCanMove(uid);
                }
            }
        }
    }
}
