using Content.Shared.Mobs.Components;
using Content.Shared.Mobs;
using Robust.Shared.Map;
using Robust.Shared.Timing;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Audio;
using Content.Shared.Jittering;
using Content.Shared.Weapons.Melee;
using Content.Shared.Movement.Components;

namespace Content.Shared.Imperial.Mobs.Phantomor
{
    public sealed class PhantomorTentacleSystem : EntitySystem
    {
        [Dependency] private readonly SharedTransformSystem _transform = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly SharedAudioSystem _audio = default!;
        [Dependency] private readonly SharedJitteringSystem _jitterSystem = default!;

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
            if (args.LastTeleport.TryGetValue(entity, out var lastTelep) && curTime - lastTelep < args.TeleportCooldown)
                return;

            if (AttemptTeleportPhantomor(entity, args))
            {
                args.LastTeleport[entity] = curTime;
                args.Handled = true;
            }
        }

        private bool AttemptTeleportPhantomor(EntityUid entityTeleport, PhantomorSummonTentacleAction args)
        {
            if (!Exists(entityTeleport))
                return false;

            if (!EntityManager.TryGetComponent(entityTeleport, out TransformComponent? playerTransform))
                return false;

            var playerPosition = playerTransform.WorldPosition;
            var mapId = playerTransform.MapID;
            var mapCoordinates = new MapCoordinates(playerPosition, mapId);
            var nearbyEntities = EntitySystem.Get<EntityLookupSystem>().GetEntitiesInRange(mapCoordinates, 10f);

            // поиск ближайшей цели для телепорта
            EntityUid? targetEntity = null;
            var nearestDistance = float.MaxValue;

            foreach (var uid in nearbyEntities)
            {
                if (!Exists(uid))
                    continue;

                if (uid == entityTeleport)
                    continue;

                if (!TryComp<MobStateComponent>(uid, out var mobState))
                    continue;

                if (mobState.CurrentState != MobState.Alive)
                    continue;

                if (!TryComp<TransformComponent>(uid, out var transform))
                    continue;

                var distance = (transform.WorldPosition - playerTransform.WorldPosition).Length();
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
                var blockComp = EnsureComp<PhantomorMovementBlockComponent>(entityTeleport);
                blockComp.AttackBlocked = true;
                blockComp.AttackBlockedUntil = _gameTiming.CurTime + args.FreezeAttack;
                meleeWeapon.NextAttack = blockComp.AttackBlockedUntil;
                Dirty(entityTeleport, meleeWeapon);

                var beforeEv = new BeforeMovementBlockedEvent(entityTeleport, true, false);
                RaiseLocalEvent(entityTeleport, ref beforeEv);

                var changedEv = new MovementBlockChangedEvent(entityTeleport, true);
                RaiseLocalEvent(entityTeleport, ref changedEv);

                var afterEv = new AfterMovementBlockedEvent(entityTeleport, true);
                RaiseLocalEvent(entityTeleport, ref afterEv);
            }

            // временная блокировка движения
            if (TryComp<InputMoverComponent>(entityTeleport, out var mover))
            {
                var blockComp = EnsureComp<PhantomorMovementBlockComponent>(entityTeleport);
                blockComp.WalkBlocked = true;
                blockComp.WalkBlockedUntil = _gameTiming.CurTime + args.FreezeWalking;
                Dirty(entityTeleport, blockComp);

                mover.CanMove = false;
                Dirty(entityTeleport, mover);

                var beforeEv = new BeforeMovementBlockedEvent(entityTeleport, true, false);
                RaiseLocalEvent(entityTeleport, ref beforeEv);

                var changedEv = new MovementBlockChangedEvent(entityTeleport, true);
                RaiseLocalEvent(entityTeleport, ref changedEv);

                var afterEv = new AfterMovementBlockedEvent(entityTeleport, true);
                RaiseLocalEvent(entityTeleport, ref afterEv);
            }

            _audio.PlayPvs(args.TeleportSound, entityTeleport, AudioParams.Default.WithVolume(-2f));
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
            var query = EntityQueryEnumerator<PhantomorMovementBlockComponent, InputMoverComponent, MeleeWeaponComponent>();

            while (query.MoveNext(out var uid, out var blockComp, out var mover, out var melee))
            {
                if (!Exists(uid))
                    continue;

                // разблокировка ходьбы
                if (blockComp.WalkBlocked && curTime >= blockComp.WalkBlockedUntil)
                {
                    blockComp.WalkBlocked = false;
                    Dirty(uid, blockComp);

                    mover.CanMove = true;
                    Dirty(uid, mover);

                    var changedEv = new MovementBlockChangedEvent(uid, false);
                    RaiseLocalEvent(uid, ref changedEv);
                }

                // разблокировка атаки
                if (blockComp.AttackBlocked && curTime >= blockComp.AttackBlockedUntil)
                {
                    blockComp.AttackBlocked = false;
                    Dirty(uid, blockComp);

                    melee.NextAttack = curTime;
                    Dirty(uid, melee);

                    var changedEv = new MovementBlockChangedEvent(uid, false);
                    RaiseLocalEvent(uid, ref changedEv);
                }
            }
        }
    }
}
