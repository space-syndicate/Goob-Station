using Content.Shared.Coordinates;
using Content.Shared.Hands;
using Content.Shared.Imperial.MiningWeapons.EmpoweredAttacks.EarthshakerStrike;
using Content.Shared.Imperial.MiningWeapons.EmpoweredAttacks.EarthshakerStrike.Events;
using Robust.Shared.Map.Components;

namespace Content.Shared.Imperial.MiningWeapons.EmpoweredAttacks;

public abstract partial class SharedAttacksSystem
{
    private void InitializeEarthshakerStrike()
    {
        SubscribeLocalEvent<UserEarthshakerStrikeComponent, EarthshakerStrikeEvent>(OnEarthshakerStrike);

        SubscribeLocalEvent<EarthshakerStrikeComponent, EarthshakerStrikeDoAfterEvent>(OnEarthshakerStrikeDoAfter);
        SubscribeLocalEvent<EarthshakerStrikeComponent, GotEquippedHandEvent>(OnEquippedEarthshakerStrike);
        SubscribeLocalEvent<EarthshakerStrikeComponent, GotUnequippedHandEvent>(OnUnequippedEarthshakerStrike);
        SubscribeLocalEvent<EarthshakerStrikeComponent, ComponentShutdown>(OnEarthshakerShutdown);
    }


    private void OnEarthshakerStrike(EntityUid user, UserEarthshakerStrikeComponent comp, ref EarthshakerStrikeEvent args)
    {
        if (args.Handled)
            return;

        if (!comp.Item.HasValue)
            return;

        if (!IsItemWielded(comp.Item.Value))
        {
            ItemWieldedCancelled(user);
            return;
        }

        if (comp.HasDoAfter)
        {
            _audio.PlayPvs(comp.StartDoAfterSound, user);
            if (!StartDoAfter(user, comp.Item.Value, comp.DoAfterTime, new EarthshakerStrikeDoAfterEvent()))
                return;
        }
        else
        {
            _audio.PlayPvs(comp.CompletedSound, user);
            Spawn(comp.EarthshakerRiftSpawnPrototype, user.ToCoordinates());
        }

        args.Handled = true;
    }

    private void OnEarthshakerStrikeDoAfter(EntityUid uid, EarthshakerStrikeComponent comp, EarthshakerStrikeDoAfterEvent args)
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

        var coords = uid.ToCoordinates();
        var pos = _transform.ToMapCoordinates(coords);

        if (_transform.GetGrid(coords) is not { } grid || !TryComp<MapGridComponent>(grid, out var gridComp))
            return;

        if (!_mapSystem.TryGetTileRef(grid, gridComp, coords, out var tileRef) ||
            _turf.IsSpace(tileRef))
        {
            return;
        }

        if (_net.IsServer)
            Spawn(comp.EarthshakerRiftSpawnPrototype, pos);

        _audio.PlayPvs(comp.CompletedSound, uid);

        args.Handled = true;
    }

    private void OnEquippedEarthshakerStrike(EntityUid uid, EarthshakerStrikeComponent comp, EquippedHandEvent args)
    {
        _action.AddAction(args.User, ref comp.Action, comp.ActionEarthshakerStrike);

        if (comp.Action != null)
            _action.StartUseDelay(comp.Action.Value);

        var userComp = EnsureComp<UserEarthshakerStrikeComponent>(args.User);
        userComp.EarthshakerRiftSpawnPrototype = comp.EarthshakerRiftSpawnPrototype;
        userComp.DoAfterTime = comp.DoAfterTime;
        userComp.HasDoAfter = comp.HasDoAfter;
        userComp.CompletedSound = comp.CompletedSound;
        userComp.StartDoAfterSound = comp.StartDoAfterSound;
        userComp.Item = uid;

        comp.User = args.User;
    }

    private void OnUnequippedEarthshakerStrike(EntityUid uid, EarthshakerStrikeComponent comp, UnequippedHandEvent args)
    {
        if (comp.Action != null)
        {
            _action.RemoveAction(args.User, comp.Action.Value);
            comp.Action = null;

            RemComp<UserEarthshakerStrikeComponent>(args.User);
        }
    }

    private void OnEarthshakerShutdown(EntityUid uid, EarthshakerStrikeComponent comp, ComponentShutdown args)
    {
        if (comp.Action != null && TryComp(uid, out TransformComponent? transform) &&
            transform.ParentUid.IsValid())
        {
            _action.RemoveAction(transform.ParentUid, comp.Action.Value);

            if (comp.User.HasValue)
                RemComp<UserEarthshakerStrikeComponent>(comp.User.Value);
        }
    }
}
