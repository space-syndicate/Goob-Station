using Content.Shared.Coordinates;
using Content.Shared.Hands;
using Content.Shared.Imperial.Damage;
using Content.Shared.Imperial.Lavaland.MiningWeapons.EmpoweredAttacks.PiercingLunge;
using Content.Shared.Imperial.Lavaland.MiningWeapons.EmpoweredAttacks.PiercingLunge.Events;
using Content.Shared.Stunnable;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Components;

namespace Content.Shared.Imperial.Lavaland.MiningWeapons.EmpoweredAttacks;

public abstract partial class SharedAttacksSystem
{
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

        if (!IsItemWielded(userComp.Item.Value))
        {
            ItemWieldedCancelled(user);
            return;
        }

        var userXform = Transform(user);
        var targetMap = _transform.ToMapCoordinates(args.Target);
        if (targetMap.MapId != userXform.MapID)
            return;

        var userPos = _transform.GetWorldPosition(userXform);
        userComp.Direction = (targetMap.Position - userPos).Normalized();

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
            if (!StartDoAfter(user, userComp.Item.Value, userComp.DoAfterTime, new PiercingLungeDoAfterEvent()))
                return;
        }
        else
        {
            if (TryComp<PiercingLungeComponent>(userComp.Item.Value, out var comp))
            {
                comp.Direction = userComp.Direction;
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
            comp.Direction = userComp.Direction;
            PiercingLunge(uid, comp.User.Value, comp);
        }

        args.Handled = true;
    }

    private void OnEquippedPiercingLunge(EntityUid uid, PiercingLungeComponent comp, GotEquippedHandEvent args)
    {
        _action.AddAction(args.User, ref comp.Action, comp.ActionPiercingLunge);

        var userComp = EnsureComp<UserPiercingLungeComponent>(args.User);
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
        if (comp.User.HasValue && comp.IsInEffect)
            RemoveContactComponents(comp.User.Value);

        if (comp.Action != null && TryComp(uid, out TransformComponent? transform) &&
            transform.ParentUid.IsValid())
        {
            _action.RemoveAction(transform.ParentUid, comp.Action.Value);

            if (comp.User.HasValue)
                RemComp<UserPiercingLungeComponent>(comp.User.Value);
        }
    }

    private void PiercingLunge(EntityUid item, EntityUid user, PiercingLungeComponent comp)
    {
        if (!TryComp<PhysicsComponent>(user, out var physics))
            return;

        comp.IsLunging = true;
        comp.LungeAccumulator = 0f;

        _stun.TryAddStunDuration(user, comp.StunTime);

        if (IsItemWielded(item))
            _physics.SetLinearVelocity(user, comp.Direction * comp.InitialLungeStrength, body: physics);
        else
            _physics.SetLinearVelocity(user, comp.Direction * comp.InitialLungeStrength / 2, body: physics);

        EnsureComp<StunOnContactComponent>(user);
        var damageContacts = EnsureComp<ImperialDamageOnCollideComponent>(user);
        damageContacts.Damage = comp.Damage;

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

            comp.LungeAccumulator += frameTime;

            if (comp.LungeAccumulator <= comp.LungeDuration)
            {
                if (!TryComp<PhysicsComponent>(comp.User.Value, out var physics))
                    continue;

                var progress = comp.LungeAccumulator / comp.LungeDuration;
                var easedProgress = (float)Math.Sin(progress * Math.PI * 0.5f);
                var currentStrength = MathHelper.Lerp(comp.InitialLungeStrength, comp.FinalLungeStrength, easedProgress);

                _physics.SetLinearVelocity(comp.User.Value, comp.Direction * currentStrength, body: physics);
            }
            else
            {
                comp.IsLunging = false;
                comp.LungeAccumulator = 0f;

                RemoveContactComponents(comp.User.Value);

                if (TryComp<PhysicsComponent>(comp.User.Value, out var physics))
                {
                    var currentVelocity = physics.LinearVelocity;
                    var stopVelocity = currentVelocity * 0.2f;
                    _physics.SetLinearVelocity(comp.User.Value, stopVelocity, body: physics);
                }
            }
        }
    }

    private void RemoveContactComponents(EntityUid entity)
    {
        if (HasComp<StunOnContactComponent>(entity))
            RemComp<StunOnContactComponent>(entity);

        if (HasComp<ImperialDamageOnCollideComponent>(entity))
            RemComp<ImperialDamageOnCollideComponent>(entity);
    }
}
