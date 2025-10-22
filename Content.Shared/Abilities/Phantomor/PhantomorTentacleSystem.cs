using Content.Shared.Mobs.Components;
using Content.Shared.Mobs;
using Content.Shared.Mind.Components;
using Robust.Shared.Map;
using Content.Shared.Teleportation;
using Robust.Shared.Timing;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Audio;
using Content.Shared.Jittering;
using Content.Shared.Weapons.Melee;
using Content.Shared.Movement.Systems;
using Content.Shared.Movement.Components;

namespace Content.Server.Teleportation
{
    public sealed class PhantomorTentacleSystem : EntitySystem
    {
        [Dependency] private readonly SharedTransformSystem _transform = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly SharedAudioSystem _audio = default!;
        [Dependency] private readonly SharedJitteringSystem _jitterSystem = default!;
        [Dependency] private readonly SharedMoverController _mover = default!;

        private readonly Dictionary<EntityUid, TimeSpan> _lastTeleport = new();
        private readonly Dictionary<EntityUid, TimeSpan> _movementBlockedUntil = new();

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<PhantomorSummonTentacleAction>(OnTentacleAction);
        }

        private void OnTentacleAction(PhantomorSummonTentacleAction args)
        {
            if (args.Handled)
                return;

            var entity = args.Performer;
            var curTime = _gameTiming.CurTime;

            // кд между ивентами 
            if (_lastTeleport.TryGetValue(entity, out var lastTelep) && (curTime - lastTelep) < TimeSpan.FromSeconds(args.teleportCooldown))
                return;

            if (TeleportPhantomor(entity, args))
            {
                _lastTeleport[entity] = curTime;
                args.Handled = true;
            }
        }

        public bool TeleportPhantomor(EntityUid entityTeleport, PhantomorSummonTentacleAction args)
        {
            if (!Exists(entityTeleport))
                return false;

            var playerTransform = Transform(entityTeleport);
            var playerPosition = playerTransform.WorldPosition;

            if (playerTransform.MapID == MapId.Nullspace)
                return false;

            // поиск ближайшей цели для телепорта
            EntityUid? targetEntity = null;
            float nearestDistance = float.MaxValue;

            var query = EntityQueryEnumerator<MindContainerComponent, MobStateComponent, TransformComponent>();
            while (query.MoveNext(out var uid, out var mindContainer, out var mobState, out var transform))
            {
                if (uid == entityTeleport)
                    continue;

                if (mobState.CurrentState != MobState.Alive)
                    continue;

                if (transform.MapID != playerTransform.MapID)
                    continue;

                var distance = (transform.WorldPosition - playerPosition).Length();
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

            // временная блокировка атаки
            if (TryComp<MeleeWeaponComponent>(entityTeleport, out var meleeWeapon))
            {
                meleeWeapon.NextAttack = _gameTiming.CurTime + TimeSpan.FromSeconds(args.freezeAttack);
                Dirty(entityTeleport, meleeWeapon);
            }

            // временная блокировка движения
            if (TryComp<InputMoverComponent>(entityTeleport, out var mover))
            {
                mover.CanMove = false;
                _movementBlockedUntil[entityTeleport] = _gameTiming.CurTime + TimeSpan.FromSeconds(args.freezeWalking);
                Dirty(entityTeleport, mover);
            }

            _audio.PlayPvs(args.teleportSound, entityTeleport, AudioParams.Default.WithVolume(-2f));
            _jitterSystem.DoJitter(entityTeleport, TimeSpan.FromSeconds(10), refresh: true, amplitude: 20f, frequency: 6f);

            // поворот к цели после телепорта
            var directionToTarget = (targetTransform.WorldPosition - Transform(entityTeleport).WorldPosition).Normalized();
            var angleToTarget = Math.Atan2(directionToTarget.Y, directionToTarget.X);
            _transform.SetWorldRotation(entityTeleport, angleToTarget);

            return true;
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            var curTime = _gameTiming.CurTime;
            var toRemove = new List<EntityUid>();

            // разбан движения после истечения времени блокировки
            foreach (var (entity, blockUntil) in _movementBlockedUntil)
            {
                if (curTime >= blockUntil)
                {
                    toRemove.Add(entity);
                    if (TryComp<InputMoverComponent>(entity, out var mover))
                    {
                        mover.CanMove = true;
                        Dirty(entity, mover);
                    }
                }
            }
            foreach (var entity in toRemove)
            {
                _movementBlockedUntil.Remove(entity);
            }
        }
    }
}
