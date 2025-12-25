using System.Numerics;
using Content.Shared.Hands;
using Content.Shared.Imperial.Lavaland.MiningWeapons.EmpoweredAttacks.EnhancedShot;
using Content.Shared.Imperial.Lavaland.MiningWeapons.EmpoweredAttacks.EnhancedShot.Events;

namespace Content.Shared.Imperial.Lavaland.MiningWeapons.EmpoweredAttacks;

public abstract partial class SharedAttacksSystem
{
    private void InitializeEnhancedShot()
    {
        SubscribeLocalEvent<UserEnhancedShotComponent, EnhancedShotEvent>(OnEnhancedShot);
        SubscribeLocalEvent<EnhancedShotComponent, EnhancedShotDoAfterEvent>(OnEnhancedShotDoAfter);
        SubscribeLocalEvent<EnhancedShotComponent, GotEquippedHandEvent>(OnEquippedEnhancedShot);
        SubscribeLocalEvent<EnhancedShotComponent, GotUnequippedHandEvent>(OnUnequippedEnhancedShot);
        SubscribeLocalEvent<EnhancedShotComponent, ComponentShutdown>(OnEnhancedShotShutdown);
    }


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
}
