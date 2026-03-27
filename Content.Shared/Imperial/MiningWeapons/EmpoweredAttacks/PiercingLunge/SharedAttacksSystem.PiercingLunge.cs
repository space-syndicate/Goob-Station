using Content.Shared.Coordinates;
using Content.Shared.Damage;
using Content.Shared.Hands;
using Content.Shared.Imperial.Damage;
using Content.Shared.Imperial.MiningWeapons.EmpoweredAttacks.PiercingLunge;
using Content.Shared.Imperial.MiningWeapons.EmpoweredAttacks.PiercingLunge.Events;
using Content.Shared.Stunnable;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Timing;

namespace Content.Shared.Imperial.MiningWeapons.EmpoweredAttacks;

public abstract partial class SharedAttacksSystem
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;

    private void InitializePiercingLunge()
    {
        SubscribeLocalEvent<UserPiercingLungeComponent, PiercingLungeEvent>(OnPiercingLunge);

        SubscribeLocalEvent<PiercingLungeComponent, PiercingLungeDoAfterEvent>(OnPiercingLungeDoAfter);
        SubscribeLocalEvent<PiercingLungeComponent, GotEquippedHandEvent>(OnEquippedPiercingLunge);
        SubscribeLocalEvent<PiercingLungeComponent, GotUnequippedHandEvent>(OnUnequippedPiercingLunge);
        SubscribeLocalEvent<PiercingLungeComponent, ComponentShutdown>(OnPiercingLungeShutdown);
    }

    private void OnPiercingLunge(EntityUid user, UserPiercingLungeComponent userComp, ref PiercingLungeEvent args)
    {
        if (args.Handled)
            return;

        if (!userComp.Item.HasValue)
            return;

        if (!_miningWeaponsHelpers.IsItemWielded(userComp.Item.Value))
        {
            ItemWieldedCancelled(user);
            return;
        }

        var userXform = Transform(user);
        var targetMap = _transform.ToMapCoordinates(args.Target);
        if (targetMap.MapId != userXform.MapID)
            return;

        var userPos = _transform.GetWorldPosition(userXform);
        userComp.Direction = ((targetMap.Position - userPos).Normalized()).ToAngle();

        var coords = userComp.Item.Value.ToCoordinates();
        if (_transform.GetGrid(coords) is not { } grid || !TryComp<MapGridComponent>(grid, out var gridComp))
            return;

        if (!_mapSystem.TryGetTileRef(grid, gridComp, coords, out var tileRef) ||
            _turf.IsSpace(tileRef))
        {
            return;
        }

        if (HasComp<KnockedDownComponent>(user))
            return;

        if (userComp.HasDoAfter)
        {
            if (!StartDoAfter(user, userComp.Item.Value, (float)userComp.DoAfterTime.TotalSeconds, new PiercingLungeDoAfterEvent()))
                return;
        }
        else
        {
            if (TryComp<PiercingLungeComponent>(userComp.Item.Value, out var comp))
            {
                comp.Direction = userComp.Direction.ToVec();
                PiercingLunge(userComp.Item.Value, user, comp);
            }
        }

        args.Handled = true;
    }

    private void OnPiercingLungeDoAfter(EntityUid uid, PiercingLungeComponent comp, PiercingLungeDoAfterEvent args)
    {
        if (!comp.User.HasValue)
            return;

        if (args.Cancelled)
        {
            DoAfterCancelled(comp.User.Value);
            return;
        }

        if (args.Handled)
            return;

        if (!_net.IsServer)
            return;

        if (TryComp<UserPiercingLungeComponent>(comp.User.Value, out var userComp))
        {
            comp.Direction = userComp.Direction.ToVec();
            PiercingLunge(uid, comp.User.Value, comp);
        }

        args.Handled = true;
    }

    private void OnEquippedPiercingLunge(EntityUid uid, PiercingLungeComponent comp, GotEquippedHandEvent args)
    {
        _action.AddAction(args.User, ref comp.Action, comp.ActionPiercingLunge);

        if (comp.Action != null)
            _action.StartUseDelay(comp.Action.Value);

        var userComp = EnsureComp<UserPiercingLungeComponent>(args.User);
        userComp.Damage = comp.Damage;
        userComp.DoAfterTime = comp.DoAfterTime;
        userComp.HasDoAfter = comp.HasDoAfter;
        userComp.Item = uid;

        comp.User = args.User;
    }

    private void OnUnequippedPiercingLunge(EntityUid uid, PiercingLungeComponent comp, GotUnequippedHandEvent args)
    {
        if (comp.Action != null)
        {
            _action.RemoveAction(args.User, comp.Action.Value);
            comp.Action = null;

            RemComp<UserPiercingLungeComponent>(args.User);
        }
    }

    private void OnPiercingLungeShutdown(EntityUid uid, PiercingLungeComponent comp, ComponentShutdown args)
    {
        if (!comp.User.HasValue)
            return;

        if (comp.Action != null && TryComp(uid, out TransformComponent? transform) &&
            transform.ParentUid.IsValid())
        {
            _action.RemoveAction(transform.ParentUid, comp.Action.Value);

            RemComp<UserPiercingLungeComponent>(comp.User.Value);
        }
    }

    private void PiercingLunge(EntityUid item, EntityUid user, PiercingLungeComponent comp)
    {
        if (!TryComp<PhysicsComponent>(user, out var physics))
            return;

        comp.IsInEffect = true;
        comp.IsLunging = true;
        comp.LungeAccumulator = TimeSpan.Zero;

        _stun.TryAddStunDuration(user, comp.StunTime);

        if (_miningWeaponsHelpers.IsItemWielded(item))
            _physics.SetLinearVelocity(user, comp.Direction * comp.InitialLungeStrength, body: physics);
        else
            _physics.SetLinearVelocity(user, comp.Direction * comp.InitialLungeStrength / 2, body: physics);

        if (TryComp<UserPiercingLungeComponent>(user, out var userComp))
            userComp.CanDamage = true;

        _audio.PlayPvs(comp.CompletedSound, user);
    }

    private void UpdatePiercingLunge(float frameTime)
    {
        var query = EntityQueryEnumerator<PiercingLungeComponent>();
        while (query.MoveNext(out var _, out var comp))
        {
            if (!comp.User.HasValue)
                continue;

            if (!comp.IsLunging)
                continue;

            var user = comp.User.Value;
            comp.LungeAccumulator += _gameTiming.FrameTime;

            if (comp.LungeAccumulator <= TimeSpan.FromSeconds(comp.LungeDuration))
            {
                if (!TryComp<PhysicsComponent>(user, out var physics))
                    continue;

                double progress = comp.LungeAccumulator.TotalSeconds / comp.LungeDuration;
                var easedProgress = (float)Math.Sin(progress * Math.PI * 0.5f);
                var currentStrength = MathHelper.Lerp(comp.InitialLungeStrength, comp.FinalLungeStrength, easedProgress);

                _physics.SetLinearVelocity(user, comp.Direction * currentStrength, body: physics);
            }
            else
            {
                comp.IsLunging = false;
                comp.LungeAccumulator = TimeSpan.Zero;

                if (TryComp<UserPiercingLungeComponent>(user, out var userComp))
                    userComp.CanDamage = false;

                if (TryComp<PhysicsComponent>(user, out var physics))
                {
                    var currentVelocity = physics.LinearVelocity;
                    var stopVelocity = currentVelocity * 0.2f;
                    _physics.SetLinearVelocity(user, stopVelocity, body: physics);
                }
            }
        }
    }
}
