using System.Numerics;
using Content.Shared.Hands;
using Content.Shared.Imperial.Lavaland.MiningWeapons.EmpoweredAttacks.EnhancedShot;
using Content.Shared.Imperial.Lavaland.MiningWeapons.EmpoweredAttacks.EnhancedShot.Events;
using Content.Shared.Timing;
using Content.Shared.Weapons.Ranged.Events;

namespace Content.Shared.Imperial.Lavaland.MiningWeapons.EmpoweredAttacks;

public abstract partial class SharedAttacksSystem
{
    [Dependency] private readonly UseDelaySystem _delay = default!;

    private void InitializeEnhancedShot()
    {
        SubscribeLocalEvent<UserEnhancedShotComponent, EnhancedShotEvent>(OnEnhancedShot);
        SubscribeLocalEvent<UserEnhancedShotComponent, ShotAttemptedEvent>(OnAttack);

        SubscribeLocalEvent<EnhancedShotComponent, EnhancedShotDoAfterEvent>(OnEnhancedShotDoAfter);
        SubscribeLocalEvent<EnhancedShotComponent, GotEquippedHandEvent>(OnEquippedEnhancedShot);
        SubscribeLocalEvent<EnhancedShotComponent, GotUnequippedHandEvent>(OnUnequippedEnhancedShot);
        SubscribeLocalEvent<EnhancedShotComponent, ComponentShutdown>(OnEnhancedShotShutdown);
    }


    private void OnEnhancedShot(EntityUid user, UserEnhancedShotComponent userComp, ref EnhancedShotEvent args)
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

        userComp.IsChargedAttackActive = true;

        if (TryComp<UseDelayComponent>(userComp.Item.Value, out var useDelay))
            _delay.TryResetDelay((userComp.Item.Value, useDelay));

        var userXform = Transform(user);
        var targetMap = _transform.ToMapCoordinates(args.Target);
        if (targetMap.MapId != userXform.MapID)
            return;

        var userPos = _transform.GetWorldPosition(userXform);
        userComp.Direction = (targetMap.Position - userPos).Normalized();

        if (userComp.HasDoAfter)
        {
            _audio.PlayPvs(userComp.StartDoAfterSound, user);

            if (!StartDoAfter(user, userComp.Item.Value, userComp.DoAfterTime, new EnhancedShotDoAfterEvent()))
            {
                userComp.IsChargedAttackActive = false;
                return;
            }
        }
        else
        {
            if (!TryComp<EnhancedShotComponent>(userComp.Item.Value, out var comp))
            {
                userComp.IsChargedAttackActive = false;
                return;
            }

            ShootEnhancedProjectile(userComp.Item.Value, user, comp);
        }

        args.Handled = true;
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

        if (comp.Action != null)
            _action.StartUseDelay(comp.Action.Value);

        var userComp = EnsureComp<UserEnhancedShotComponent>(args.User);
        userComp.DoAfterTime = comp.DoAfterTime;
        userComp.HasDoAfter = comp.HasDoAfter;
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

    private void OnAttack(EntityUid user, UserEnhancedShotComponent component, ShotAttemptedEvent args)
    {
        if (component.IsChargedAttackActive)
        {
            args.Cancel();
            return;
        }

        if (!component.Item.HasValue)
            return;

        if (TryComp<UseDelayComponent>(component.Item.Value, out var useDelay) &&
            _delay.IsDelayed((component.Item.Value, useDelay)))
        {
            args.Cancel();
            return;
        }
    }

    private void ShootEnhancedProjectile(EntityUid weaponUid, EntityUid user, EnhancedShotComponent comp)
    {
        if (!TryComp<UserEnhancedShotComponent>(user, out var userComp))
            return;

        var xform = Transform(weaponUid);
        var fromCoords = xform.Coordinates;
        var fromMap = _transform.ToMapCoordinates(fromCoords);
        var projectile = Spawn(comp.ProjectilePrototype, fromMap);

        var direction = userComp.Direction.Normalized();
        _gunSystem.ShootProjectile(projectile, direction, Vector2.Zero, weaponUid, user, speed: comp.ProjectileSpeed);
        _sharedCameraRecoil.KickCamera(user, -direction);

        _audio.PlayPvs(comp.ShotSound, user);

        if (TryComp<UseDelayComponent>(weaponUid, out var useDelay))
            _delay.TryResetDelay((weaponUid, useDelay));

        userComp.IsChargedAttackActive = false;
    }
}
