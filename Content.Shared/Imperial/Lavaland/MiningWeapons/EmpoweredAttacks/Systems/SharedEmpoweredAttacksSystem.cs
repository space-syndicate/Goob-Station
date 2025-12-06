using Content.Shared.Actions;
using Content.Shared.Hands;
using Content.Shared.Popups;
using Content.Shared.DoAfter;
using Content.Shared.Wieldable.Components;
using Content.Shared.Imperial.Lavaland.MiningWeapons.EmpoweredAttacks.Events;
using Content.Shared.Imperial.Lavaland.MiningWeapons.EmpoweredAttacks.Components;
using Content.Shared.Coordinates;
using Content.Shared.Stunnable;
using Robust.Shared.Map.Components;
using Robust.Shared.Network;
using Content.Shared.Maps;
using Robust.Shared.Physics.Components;
using Content.Shared.Camera;
using Robust.Shared.Physics.Systems;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Audio.Systems;
using Content.Shared.Imperial.Damage;
using System.Numerics;


namespace Content.Shared.Imperial.Lavaland.MiningWeapons.EmpoweredAttacks.Systems;

public abstract class SharedEmpoweredAttacksSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _action = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedGunSystem _gunSystem = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedMapSystem _mapSystem = default!;
    [Dependency] private readonly TurfSystem _turf = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedCameraRecoilSystem _sharedCameraRecoil = default!;

    public override void Initialize()
    {
        base.Initialize();

        // Earthshaker
        SubscribeLocalEvent<UserEarthshakerStrikeComponent, EarthshakerStrikeEvent>(OnEarthshakerStrike);
        SubscribeLocalEvent<EarthshakerStrikeComponent, EarthshakerStrikeDoAfterEvent>(OnEarthshakerStrikeDoAfter);
        SubscribeLocalEvent<EarthshakerStrikeComponent, GotEquippedHandEvent>(OnEquippedEarthshakerStrike);
        SubscribeLocalEvent<EarthshakerStrikeComponent, GotUnequippedHandEvent>(OnUnequippedEarthshakerStrike);
        SubscribeLocalEvent<EarthshakerStrikeComponent, ComponentShutdown>(OnEarthshakerShutdown);

        // Enhanced Shot
        SubscribeLocalEvent<UserEnhancedShotComponent, EnhancedShotEvent>(OnEnhancedShot);
        SubscribeLocalEvent<EnhancedShotComponent, EnhancedShotDoAfterEvent>(OnEnhancedShotDoAfter);
        SubscribeLocalEvent<EnhancedShotComponent, GotEquippedHandEvent>(OnEquippedEnhancedShot);
        SubscribeLocalEvent<EnhancedShotComponent, GotUnequippedHandEvent>(OnUnequippedEnhancedShot);
        SubscribeLocalEvent<EnhancedShotComponent, ComponentShutdown>(OnEnhancedShotShutdown);

        // Piercing Lunge
        SubscribeLocalEvent<UserPiercingLungeComponent, PiercingLungeEvent>(OnPiercingLunge);
        SubscribeLocalEvent<PiercingLungeComponent, PiercingLungeDoAfterEvent>(OnPiercingLungeDoAfter);
        SubscribeLocalEvent<PiercingLungeComponent, GotEquippedHandEvent>(OnEquippedPiercingLunge);
        SubscribeLocalEvent<PiercingLungeComponent, GotUnequippedHandEvent>(OnUnequippedPiercingLunge);
        SubscribeLocalEvent<PiercingLungeComponent, ComponentShutdown>(OnPiercingLungeShutdown);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        UpdatePiercingLunge(frameTime);
    }

    #region Earthshaker Strike Logic

    private void OnEarthshakerStrike(EntityUid user, UserEarthshakerStrikeComponent comp, ref EarthshakerStrikeEvent args)
    {
        if (!comp.Item.HasValue)
            return;

        if (comp.HasWielded && !IsItemWielded(comp.Item.Value))
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

        var userComp = EnsureComp<UserEarthshakerStrikeComponent>(args.User);
        userComp.EarthshakerRiftSpawnPrototype = comp.EarthshakerRiftSpawnPrototype;
        userComp.DoAfterTime = comp.DoAfterTime;
        userComp.HasDoAfter = comp.HasDoAfter;
        userComp.HasWielded = comp.HasWielded;
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

    #endregion

    #region Enhanced Shot Logic

    private void OnEnhancedShot(EntityUid user, UserEnhancedShotComponent comp, ref EnhancedShotEvent args)
    {
        if (!comp.Item.HasValue)
            return;

        var userXform = Transform(user);
        var targetMap = _transform.ToMapCoordinates(args.Target);
        if (targetMap.MapId != userXform.MapID)
            return;

        var userPos = _transform.GetWorldPosition(userXform);
        comp.Direction = (targetMap.Position - userPos).Normalized();

        _audio.PlayPvs(comp.StartDoAfterSound, user);

        if (!StartDoAfter(user, comp.Item.Value, comp.DoAfterTime, new EnhancedShotDoAfterEvent()))
            return;
    }

    private void OnEnhancedShotDoAfter(EntityUid uid, EnhancedShotComponent comp, EnhancedShotDoAfterEvent args)
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

        ShootEnhancedProjectile(uid, comp.User.Value, comp);

        args.Handled = true;
    }

    private void OnEquippedEnhancedShot(EntityUid uid, EnhancedShotComponent comp, GotEquippedHandEvent args)
    {
        _action.AddAction(args.User, ref comp.Action, comp.ActionEnhancedShot);

        var userComp = EnsureComp<UserEnhancedShotComponent>(args.User);
        userComp.DoAfterTime = comp.DoAfterTime;
        userComp.StartDoAfterSound = comp.StartDoAfterSound;
        userComp.Item = uid;

        comp.User = args.User;
    }

    private void OnUnequippedEnhancedShot(EntityUid uid, EnhancedShotComponent comp, GotUnequippedHandEvent args)
    {
        if (comp.Action != null)
        {
            _action.RemoveAction(args.User, comp.Action.Value);
            comp.Action = null;

            RemComp<UserEnhancedShotComponent>(args.User);
        }
    }

    private void OnEnhancedShotShutdown(EntityUid uid, EnhancedShotComponent comp, ComponentShutdown args)
    {
        if (comp.Action != null && TryComp(uid, out TransformComponent? transform) &&
            transform.ParentUid.IsValid())
        {
            _action.RemoveAction(transform.ParentUid, comp.Action.Value);

            if (comp.User.HasValue)
                RemComp<UserEnhancedShotComponent>(comp.User.Value);
        }
    }

    private void ShootEnhancedProjectile(EntityUid uid, EntityUid user, EnhancedShotComponent comp)
    {
        if (!TryComp<UserEnhancedShotComponent>(user, out var userComp))
            return;

        var xform = Transform(uid);
        var fromCoords = xform.Coordinates;
        var fromMap = _transform.ToMapCoordinates(fromCoords);
        var projectile = Spawn(comp.ProjectilePrototype, fromMap);

        var direction = userComp.Direction.Normalized();
        _gunSystem.ShootProjectile(projectile, direction, Vector2.Zero, uid, user, speed: comp.ProjectileSpeed);
        _sharedCameraRecoil.KickCamera(user, -direction);

        _audio.PlayPvs(comp.CompletedSound, user);
    }

    #endregion

    #region Piercing Lunge Logic

    private void OnPiercingLunge(EntityUid user, UserPiercingLungeComponent comp, ref PiercingLungeEvent args)
    {
        if (!comp.Item.HasValue)
            return;

        var userXform = Transform(user);
        var targetMap = _transform.ToMapCoordinates(args.Target);
        if (targetMap.MapId != userXform.MapID)
            return;

        var userPos = _transform.GetWorldPosition(userXform);
        comp.Direction = (targetMap.Position - userPos).Normalized();

        _audio.PlayPvs(comp.StartDoAfterSound, user);

        if (!StartDoAfter(user, comp.Item.Value, comp.DoAfterTime, new PiercingLungeDoAfterEvent()))
            return;
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

        var coords = uid.ToCoordinates();
        if (_transform.GetGrid(coords) is not { } grid || !TryComp<MapGridComponent>(grid, out var gridComp))
            return;

        if (!_mapSystem.TryGetTileRef(grid, gridComp, coords, out var tileRef) ||
            _turf.IsSpace(tileRef))
        {
            return;
        }

        if (HasComp<KnockedDownComponent>(comp.User.Value))
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
        userComp.StartDoAfterSound = comp.StartDoAfterSound;
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

    private void PiercingLunge(EntityUid uid, EntityUid user, PiercingLungeComponent comp)
    {
        if (!TryComp<PhysicsComponent>(user, out var physics))
            return;

        comp.IsLunging = true;
        comp.LungeAccumulator = 0f;

        _physics.SetLinearVelocity(user, comp.Direction * comp.InitialLungeStrength, body: physics);
        _stun.TryAddStunDuration(user, comp.StunTime);

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

    #endregion

    #region Helpers

    private bool StartDoAfter(EntityUid user, EntityUid used, float time, DoAfterEvent doAfterEvent)
    {
        var args = new DoAfterArgs(EntityManager, user, time, doAfterEvent, used)
        {
            BreakOnDamage = true,
            BreakOnMove = true
        };

        return _doAfter.TryStartDoAfter(args);
    }

    private void DoAfterCancelled(EntityUid user)
    {
        Log.Info("DoAfter сбит");

        _popup.PopupEntity(Loc.GetString("doafter-sbit"), user, user); //change
    }

    private void ItemWieldedCancelled(EntityUid user)
    {
        Log.Info("Wielded false");

        _popup.PopupEntity(Loc.GetString("item-wielded-false"), user, user); //change
    }

    private bool IsItemWielded(EntityUid item)
    {
        return TryComp<WieldableComponent>(item, out var wieldable) && wieldable.Wielded;
    }

    #endregion
}
