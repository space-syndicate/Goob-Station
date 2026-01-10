using Content.Shared.Actions;
using Content.Shared.Buckle;
using Content.Shared.Buckle.Components;
using Content.Shared.Tag;
using Content.Shared.Projectiles;
using Content.Shared.Popups;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Imperial.ImperialVehicle.Events;
using Content.Shared.Imperial.ImperialVehicle.Enums;
using Content.Shared.Imperial.ImperialVehicle.Components;
using Content.Shared.Inventory.VirtualItem;
using Content.Shared.Hands;
using Content.Shared.Foldable;
using Robust.Shared.Containers;
using Robust.Shared.Physics.Events;
using Robust.Shared.Audio.Systems;
using System.Numerics;

namespace Content.Shared.Imperial.ImperialVehicle;

public abstract partial class SharedImperialVehicleSystem : EntitySystem
{
    [Dependency] protected readonly SharedAppearanceSystem Appearance = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _modifier = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedMoverController _mover = default!;
    [Dependency] private readonly TagSystem _tagSystem = default!;
    [Dependency] private readonly SharedBuckleSystem _buckleSystem = default!;
    [Dependency] private readonly SharedVirtualItemSystem _virtualItemSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ImperialVehicleComponent, ComponentStartup>(OnVehicleStartup);
        SubscribeLocalEvent<ImperialVehicleComponent, MoveEvent>(OnMoveEvent);
        SubscribeLocalEvent<ImperialVehicleComponent, PreventCollideEvent>(OnVehiclePreventCollide);
        SubscribeLocalEvent<ImperialVehicleComponent, StrapAttemptEvent>(OnStrapAttempt);
        SubscribeLocalEvent<ImperialVehicleComponent, UnstrapAttemptEvent>(OnUnstrapAttempt);
        SubscribeLocalEvent<ImperialVehicleComponent, UnstrappedEvent>(OnUnstrapped);
        SubscribeLocalEvent<ImperialVehicleComponent, StrappedEvent>(OnStrapped);
        SubscribeLocalEvent<ImperialVehicleComponent, VirtualItemDeletedEvent>(OnVirtualItemDeleted);
        SubscribeLocalEvent<ImperialVehicleComponent, HornActionEvent>(OnHorn);

        SubscribeLocalEvent<ImperialVehiclePilotComponent, ContainerGettingInsertedAttemptEvent>(OnPilotInsertAttempt);
        SubscribeLocalEvent<ImperialVehiclePilotComponent, PreventCollideEvent>(OnPilotPreventCollide);
    }

    public override void Update(float frameTime)
    {
        var vehicleQuery = EntityQueryEnumerator<ImperialVehicleComponent, InputMoverComponent>();
        while (vehicleQuery.MoveNext(out var uid, out var vehicle, out var mover))
        {
            if (vehicle.Rider == null)
                continue;

            if (!vehicle.AutoAnimate)
                continue;

            var velocity = _mover.GetVelocityInput(mover);
            if (velocity.Sprinting == Vector2.Zero)
            {
                UpdateAutoAnimate(uid, false);
                continue;
            }

            UpdateAutoAnimate(uid, true);
        }
    }

    private void OnVehicleStartup(EntityUid uid, ImperialVehicleComponent component, ComponentStartup args)
    {
        UpdateDrawDepth(uid, 2);

        if (TryComp<StrapComponent>(uid, out var strap))
        {
            component.BaseBuckleOffset = strap.BuckleOffset;
        }

        _modifier.RefreshMovementSpeedModifiers(uid);
    }

    private void OnMoveEvent(EntityUid uid, ImperialVehicleComponent component, ref MoveEvent args)
    {
        if (args.NewRotation == args.OldRotation)
            return;

        if (component.Rider != null)
            _transform.SetLocalRotation(component.Rider.Value, Angle.Zero);

        if (component.AutoAnimate && !HasComp<InputMoverComponent>(uid))
        {
            UpdateAutoAnimate(uid, false);
            return;
        }

        if (TryComp<InputMoverComponent>(uid, out var _))
            UpdateDrawDepth(uid, GetDrawDepth(args.Component, component));
    }

    private void OnVehiclePreventCollide(EntityUid uid, ImperialVehicleComponent component, ref PreventCollideEvent args)
    {
        if (component.Rider == null)
            return;

        if (args.OtherEntity == component.Rider)
        {
            args.Cancelled = true;
            return;
        }

        var attacker = GetProjectileAttacker(args.OtherEntity);
        if (attacker == component.Rider)
        {
            args.Cancelled = true;
            return;
        }
    }

    private void OnStrapAttempt(EntityUid uid, ImperialVehicleComponent component, ref StrapAttemptEvent args)
    {
        if (component.Rider != null)
        {
            args.Cancelled = true;
            return;
        }

        if (TryComp<FoldableComponent>(uid, out var foldableComp) && foldableComp.IsFolded)
        {
            args.Cancelled = true;
            return;
        }

        args.Cancelled = false;
    }

    private void OnStrapped(EntityUid uid, ImperialVehicleComponent component, ref StrappedEvent args)
    {
        if (component.Rider != null)
            return;

        component.Rider = args.Buckle.Owner;
        component.LastRider = component.Rider;
        Dirty(uid, component);

        if (component.UseHand)
        {
            if (!_virtualItemSystem.TrySpawnVirtualItemInHand(uid, args.Buckle.Owner))
            {
                _virtualItemSystem.DeleteInHandsMatching(args.Buckle.Owner, uid);
                return;
            }
        }

        AddHorns(args.Buckle.Owner, uid);

        SetupRider(uid, args.Buckle.Owner, component);
    }

    private void OnUnstrapAttempt(EntityUid uid, ImperialVehicleComponent component, ref UnstrapAttemptEvent args)
    {
        _virtualItemSystem.DeleteInHandsMatching(args.Buckle.Owner, uid);

        RemoveRider(uid, args.Buckle.Owner, component);
    }

    private void OnUnstrapped(EntityUid uid, ImperialVehicleComponent component, ref UnstrappedEvent args)
    {
        _virtualItemSystem.DeleteInHandsMatching(args.Buckle.Owner, uid);

        RemoveRider(uid, args.Buckle.Owner, component);
    }

    private void OnVirtualItemDeleted(EntityUid uid, ImperialVehicleComponent component, VirtualItemDeletedEvent args)
    {
        RemoveRider(uid, args.User, component);
    }

    private void OnHorn(EntityUid uid, ImperialVehicleComponent component, HornActionEvent args)
    {
        if (args.Handled == true)
            return;

        if (component.Rider != args.Performer)
            return;

        if (component.HornSound == null)
            return;

        _audio.PlayPvs(component.HornSound, uid);
        args.Handled = true;
    }


    /// <summary>
    /// To avoid the problem where the user can operate the vehicle from
    /// a cabinet/trash can and other similar containers.
    /// </summary>
    private void OnPilotInsertAttempt(EntityUid uid, ImperialVehiclePilotComponent pilotComponent, ContainerGettingInsertedAttemptEvent args)
    {
        if (pilotComponent.Vehicle != null && args.Container.Owner != pilotComponent.Vehicle)
        {
            args.Cancel();
            _popup.PopupEntity(Loc.GetString("imperial-vehicle-cannot-insert-pilot"), uid, uid);
        }
    }

    /// <summary>
    /// Blocking attempts by external systems to move the pilot
    /// independently from the transport using Collide
    /// </summary>
    private void OnPilotPreventCollide(EntityUid uid, ImperialVehiclePilotComponent component, ref PreventCollideEvent args)
    {
        if (HasComp<ProjectileComponent>(args.OtherEntity))
        {
            args.Cancelled = false;
            return;
        }

        args.Cancelled = true;
    }


    public void SetupRider(EntityUid vehicleUid, EntityUid riderUid, ImperialVehicleComponent component)
    {
        EnsureComp<InputMoverComponent>(vehicleUid);

        RemComp<PullableComponent>(riderUid);

        if (TryComp<MovementSpeedModifierComponent>(vehicleUid, out var vehicleSpeedMod))
        {
            _modifier.ChangeBaseSpeed(
                vehicleUid,
                component.BaseWalkSpeed,
                component.BaseSprintSpeed,
                component.Acceleration,
                vehicleSpeedMod
            );
        }

        var rider = EnsureComp<ImperialVehiclePilotComponent>(riderUid);
        rider.Vehicle = vehicleUid;

        if (TryComp<InputMoverComponent>(riderUid, out var inputMover))
        {
            inputMover.CanMove = false;
            Dirty(riderUid, inputMover);
        }

        component.Rider = riderUid;
        component.LastRider = component.Rider;
        Dirty(vehicleUid, component);

        _transform.SetParent(riderUid, vehicleUid);
        _mover.SetRelay(riderUid, vehicleUid);

        _transform.SetLocalPosition(riderUid, Vector2.Zero);
        _transform.SetLocalRotation(riderUid, Angle.Zero);

        if (TryComp<InputMoverComponent>(vehicleUid, out var mover))
        {
            UpdateDrawDepth(vehicleUid, GetDrawDepth(Transform(vehicleUid), component));
        }

        _tagSystem.AddTag(vehicleUid, component.DoorBumpOpenerTag);

        _modifier.RefreshMovementSpeedModifiers(vehicleUid);
        _modifier.RefreshMovementSpeedModifiers(riderUid);
    }

    public void RemoveRider(EntityUid vehicleUid, EntityUid riderUid, ImperialVehicleComponent component)
    {
        if (component.Rider != riderUid)
            return;

        RemComp<RelayInputMoverComponent>(riderUid);

        EnsureComp<PullableComponent>(riderUid);

        _transform.AttachToGridOrMap(riderUid);

        _transform.SetWorldPosition(riderUid, _transform.GetWorldPosition(vehicleUid));
        _transform.SetWorldRotation(riderUid, _transform.GetWorldRotation(vehicleUid));

        if (TryComp<InputMoverComponent>(riderUid, out var inputMover))
        {
            inputMover.CanMove = true;
            Dirty(riderUid, inputMover);
        }

        RemComp<ImperialVehiclePilotComponent>(riderUid);

        Appearance.SetData(vehicleUid, VehicleVisuals.HideRider, false);
        component.Rider = null;
        Dirty(vehicleUid, component);

        _tagSystem.RemoveTag(vehicleUid, component.DoorBumpOpenerTag);

        if (component.HornAction != null)
            _actions.RemoveAction(riderUid, component.HornAction);

        _buckleSystem.TryUnbuckle(riderUid, riderUid);
    }

    private void AddHorns(EntityUid driver, EntityUid vehicle)
    {
        if (!TryComp<ImperialVehicleComponent>(vehicle, out var vehicleComp))
            return;

        if (vehicleComp.HornSound != null)
            _actions.AddAction(driver, ref vehicleComp.HornAction, vehicleComp.HornActionId, vehicle);
    }
}

