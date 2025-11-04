using Robust.Shared.Physics.Systems;
using Robust.Shared.Physics.Components;
using Content.Shared.Popups;
using Content.Shared.Stunnable;
using Content.Shared.Mobs.Components;
using Content.Shared.Imperial.Abilities.Urs.Components;
using Robust.Shared.Physics.Events;
using Content.Shared.Fluids.Components;
using Content.Shared.Damage;

namespace Content.Shared.Imperial.Abilities.Urs.Systems
{
    public sealed class UrsDashSystem : EntitySystem
    {
        [Dependency] private readonly SharedPhysicsSystem _physics = default!;
        [Dependency] private readonly SharedTransformSystem _transform = default!;
        [Dependency] private readonly SharedPopupSystem _popup = default!;
        [Dependency] private readonly SharedStunSystem _stun = default!;
        [Dependency] private readonly DamageableSystem _damageableSystem = default!;
        private readonly PhysicsComponent? _pushComp;
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<UrsDashComponent, StartCollideEvent>(OnStartCollide);
            SubscribeLocalEvent<UrsDashComponent, UrsDashAction>(OnSummonAction);
        }

        public void OnSummonAction(EntityUid uid, UrsDashComponent comp, UrsDashAction args)
        {
            var direction = _transform.GetWorldRotation(uid).ToWorldVec();
            _stun.TryAddStunDuration(uid, args.StunTime);
            _popup.PopupPredicted(Loc.GetString("Revers-dash-action-message", ("entity", args.Performer)), args.Performer, args.Performer, type: PopupType.SmallCaution);
            _physics.SetLinearVelocity(uid, direction * comp.ReversePushStrength * -1f, body: _pushComp);
            comp.IsDashing = true;
            comp.Accumulator -= comp.Accumulator;
            comp.Target = args.Target;
            args.Handled = true;
        }

        public override void Update(float frameTime)
        {
            var queryOne = EntityQueryEnumerator<UrsDashComponent>();
            while (queryOne.MoveNext(out var body, out var compOne))
            {
                if (!compOne.IsDashing == true)
                    continue;
                compOne.Accumulator += frameTime;
                if (compOne.Accumulator >= compOne.UpdateIntervalToDash && compOne.IsDashing == true)
                {
                    var targetPos = _transform.GetMapCoordinates(compOne.Target).Position;
                    var xform = Transform(body);
                    var fromCoords = xform.Coordinates;
                    var fromMap = _transform.ToMapCoordinates(fromCoords);
                    var direction = targetPos - fromMap.Position;
                    _physics.SetLinearVelocity(body, direction * compOne.PushStrength * 1.8f, body: _pushComp);
                }
                if (compOne.Accumulator >= compOne.UpdateInterval && compOne.IsDashing == true)
                {
                    compOne.Accumulator -= compOne.UpdateInterval;
                    compOne.IsDashing = false;
                }
            }
            base.Update(frameTime);
        }

        public void OnStartCollide(EntityUid uid, UrsDashComponent comp, ref StartCollideEvent args)
        {
            var otherEntity = args.OtherEntity;
            if (!HasComp<MobStateComponent>(otherEntity) && !HasComp<PuddleComponent>(otherEntity) && comp.IsDashing == true)
            {
                _stun.TryAddStunDuration(uid, TimeSpan.FromSeconds(5));
            }
            if (HasComp<MobStateComponent>(otherEntity) && comp.IsDashing == true)
            {
                _stun.TryKnockdown(otherEntity, TimeSpan.FromSeconds(5));
                _stun.TryAddStunDuration(otherEntity, TimeSpan.FromSeconds(4));
                _damageableSystem.TryChangeDamage(otherEntity, comp.Damage, origin: uid);
            }
        }

    }
}
