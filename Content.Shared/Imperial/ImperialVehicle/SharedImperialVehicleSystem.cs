using Content.Shared.Imperial.ImperialVehicle.Events;
using Content.Shared.Imperial.ImperialVehicle.Enums;
using Content.Shared.Imperial.ImperialVehicle.Components;
using Content.Shared.Buckle.Components;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Item;
using Content.Shared.Movement.Pulling.Events;
using Content.Shared.Tag;
using Content.Shared.Standing;
using Content.Shared.Traits.Assorted;
using Content.Shared.Buckle;
using Content.Shared.Inventory.VirtualItem;
using Content.Shared.Popups;
using Content.Shared.Hands;
using Content.Shared.Projectiles;
using Content.Shared.Actions;
using Content.Shared.Movement.Pulling.Systems;
using Content.Shared.Foldable;
using Robust.Shared.Containers;
using Robust.Shared.Physics.Events;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Map;
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
    [Dependency] private readonly PullingSystem _pulling = default!;
    private static readonly ProtoId<TagPrototype> DoorBumpOpenerTag = "DoorBumpOpener";
    public static readonly EntProtoId HornActionId = "ImperialActionHorn";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ImperialVehicleComponent, ComponentStartup>(OnVehicleStartup);
        SubscribeLocalEvent<ImperialVehicleComponent, MoveEvent>(OnMoveEvent);
        SubscribeLocalEvent<ImperialVehicleComponent, StrapAttemptEvent>(OnStrapAttempt);
        SubscribeLocalEvent<ImperialVehicleComponent, StrappedEvent>(OnStrapped);
        SubscribeLocalEvent<ImperialVehicleComponent, UnstrappedEvent>(OnUnstrapped);
        SubscribeLocalEvent<ImperialVehicleComponent, VirtualItemDeletedEvent>(OnVirtualItemDeleted);
        SubscribeLocalEvent<ImperialVehicleComponent, PreventCollideEvent>(OnVehiclePreventCollide);
        SubscribeLocalEvent<ImperialVehicleComponent, HornActionEvent>(OnHorn);

        SubscribeLocalEvent<ImperialVehiclePilotComponent, GettingPickedUpAttemptEvent>(OnGettingPickedUpAttempt);
        SubscribeLocalEvent<ImperialVehiclePilotComponent, MoveEvent>(OnPilotMove);
        SubscribeLocalEvent<ImperialVehiclePilotComponent, ContainerGettingInsertedAttemptEvent>(OnPilotInsertAttempt);
        SubscribeLocalEvent<ImperialVehiclePilotComponent, ContainerIsInsertingAttemptEvent>(OnPilotInsertingAttempt);

        SubscribeLocalEvent<LegsParalyzedComponent, DownedEvent>(OnLegsParalyzedDowned);
    }

    public override void Update(float frameTime)
    {
        var vehicleQuery = EntityQueryEnumerator<ImperialVehicleComponent, InputMoverComponent>();
        while (vehicleQuery.MoveNext(out var uid, out var vehicle, out var mover))
        {
            if (vehicle.Rider == null)
                continue;

            CheckRiderDistance(uid, vehicle);

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

        if (TryComp<FoldableComponent>(uid, out var foldableComp))
        {
            if (foldableComp.IsFolded)
                return;
        }

        if (component.UseHand)
        {
            if (!_virtualItemSystem.TrySpawnVirtualItemInHand(uid, args.Buckle.Owner))
            {
                args.Cancelled = true;
                _virtualItemSystem.DeleteInHandsMatching(args.Buckle.Owner, uid);
                return;
            }
        }

        AddHorns(args.Buckle.Owner, uid);

        args.Cancelled = false;
    }

    private void OnStrapped(EntityUid uid, ImperialVehicleComponent component, ref StrappedEvent args)
    {
        if (component.Rider != null)
            return;

        component.Rider = args.Buckle.Owner;
        component.LastRider = component.Rider;
        Dirty(uid, component);

        SetupRider(uid, args.Buckle.Owner, component);
    }

    private void OnUnstrapped(EntityUid uid, ImperialVehicleComponent component, ref UnstrappedEvent args)
    {
        if (component.Rider != args.Buckle.Owner)
            return;

        RemoveRider(uid, args.Buckle.Owner, component);
    }

    private void OnVirtualItemDeleted(EntityUid uid, ImperialVehicleComponent component, VirtualItemDeletedEvent args)
    {
        if (component.Rider != args.User)
            return;

        _buckleSystem.TryUnbuckle(args.User, args.User);
        RemoveRider(uid, args.User, component);
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

    private void OnPilotInsertAttempt(EntityUid uid, ImperialVehiclePilotComponent pilotComponent, ContainerGettingInsertedAttemptEvent args)
    {
        if (pilotComponent.Vehicle != null && args.Container.Owner != pilotComponent.Vehicle)
        {
            args.Cancel();
            _popup.PopupEntity(Loc.GetString("imperial-vehicle-cannot-insert-pilot"), uid, uid);
        }
    }

    private void OnPilotInsertingAttempt(EntityUid uid, ImperialVehiclePilotComponent pilotComponent, ContainerIsInsertingAttemptEvent args)
    {
        // Additional check at the insertion stage
        if (pilotComponent.Vehicle != null && args.Container.Owner != pilotComponent.Vehicle)
        {
            args.Cancel();
        }
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

    private void OnGettingPickedUpAttempt(EntityUid uid, ImperialVehiclePilotComponent component, GettingPickedUpAttemptEvent args)
    {
        if (component.Vehicle == null || component.Vehicle == args.User)
            return;

        args.Cancel();
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

        if (TryComp<InputMoverComponent>(uid, out var mover))
            UpdateDrawDepth(uid, GetDrawDepth(args.Component, component));
    }

    private void OnPilotMove(EntityUid uid, ImperialVehiclePilotComponent pilotComponent, ref MoveEvent args)
    {
        if (pilotComponent.Vehicle != null &&
            TryComp<TransformComponent>(pilotComponent.Vehicle, out var vehicleXform))
        {
            _transform.SetWorldPosition(uid, vehicleXform.WorldPosition);
            _transform.SetWorldRotation(uid, vehicleXform.WorldRotation);
        }
    }

    private void SetupRider(EntityUid vehicleUid, EntityUid riderUid, ImperialVehicleComponent component)
    {
        EnsureComp<InputMoverComponent>(vehicleUid);

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

        _mover.SetRelay(riderUid, vehicleUid);

        var riderXform = Transform(riderUid);
        riderXform.AttachParent(vehicleUid);
        riderXform.LocalPosition = Vector2.Zero;
        _transform.SetCoordinates(riderUid, new EntityCoordinates(vehicleUid, Vector2.Zero));

        Appearance.SetData(vehicleUid, VehicleVisuals.HideRider, component.HideRider);

        if (TryComp<InputMoverComponent>(vehicleUid, out var mover))
        {
            UpdateDrawDepth(vehicleUid, GetDrawDepth(Transform(vehicleUid), component));
        }

        _tagSystem.AddTag(vehicleUid, DoorBumpOpenerTag);

        _modifier.RefreshMovementSpeedModifiers(vehicleUid);
        _modifier.RefreshMovementSpeedModifiers(riderUid);
    }

    private void RemoveRider(EntityUid vehicleUid, EntityUid riderUid, ImperialVehicleComponent component)
    {
        var riderXform = Transform(riderUid);
        var vehicleXform = Transform(vehicleUid);

        RemComp<RelayInputMoverComponent>(riderUid);

        riderXform.AttachToGridOrMap();
        _transform.SetWorldPosition(riderUid, vehicleXform.WorldPosition);
        _transform.SetWorldRotation(riderUid, vehicleXform.WorldRotation);

        RemComp<ImperialVehiclePilotComponent>(riderUid);
        _tagSystem.RemoveTag(vehicleUid, DoorBumpOpenerTag);

        if (component.HornAction != null)
            _actions.RemoveAction(riderUid, component.HornAction);

        if (TryComp<InputMoverComponent>(riderUid, out var inputMover))
        {
            inputMover.CanMove = true;
            Dirty(riderUid, inputMover);
        }

        Appearance.SetData(vehicleUid, VehicleVisuals.HideRider, false);
        component.Rider = null;
        Dirty(vehicleUid, component);
    }

    private void AddHorns(EntityUid driver, EntityUid vehicle)
    {
        if (!TryComp<ImperialVehicleComponent>(vehicle, out var vehicleComp))
            return;

        if (vehicleComp.HornSound != null)
            _actions.AddAction(driver, ref vehicleComp.HornAction, HornActionId, vehicle);
    }

    private EntityUid? GetProjectileAttacker(EntityUid entity)
    {
        if (TryComp<ProjectileComponent>(entity, out var projectile))
        {
            return projectile.Shooter;
        }

        return null;
    }

    /// <summary>
    /// Set the draw depth for the sprite.
    /// </summary>
    private void UpdateDrawDepth(EntityUid uid, int drawDepth)
    {
        Appearance.SetData(uid, VehicleVisuals.DrawDepth, drawDepth);
    }

    /// <summary>
    /// Set whether the vehicle's base layer is animating or not.
    /// </summary>
    private void UpdateAutoAnimate(EntityUid uid, bool autoAnimate)
    {
        Appearance.SetData(uid, VehicleVisuals.AutoAnimate, autoAnimate);
    }

    /// <summary>
    /// Depending on which direction the vehicle is facing,
    /// change its draw depth.
    /// </summary>
    private int GetDrawDepth(TransformComponent xform, ImperialVehicleComponent component)
    {
        var vehicleDirection = xform.LocalRotation.GetDir();

        return vehicleDirection switch
        {
            Direction.North => component.NorthOver
                ? (int)DrawDepth.DrawDepth.Doors
                : (int)DrawDepth.DrawDepth.WallMountedItems,
            Direction.South => component.SouthOver
                ? (int)DrawDepth.DrawDepth.Doors
                : (int)DrawDepth.DrawDepth.WallMountedItems,
            Direction.West => component.WestOver
                ? (int)DrawDepth.DrawDepth.Doors
                : (int)DrawDepth.DrawDepth.WallMountedItems,
            Direction.East => component.EastOver
                ? (int)DrawDepth.DrawDepth.Doors
                : (int)DrawDepth.DrawDepth.WallMountedItems,
            _ => (int)DrawDepth.DrawDepth.WallMountedItems
        };
    }

    /// <summary>
    /// Sets crawl movement speed for paralyzed entities when they are downed.
    /// </summary>
    private void OnLegsParalyzedDowned(EntityUid uid, LegsParalyzedComponent component, DownedEvent args)
    {
        _modifier.ChangeBaseSpeed(
            uid,
            component.CrawlMoveSpeed,
            component.CrawlMoveSpeed,
            component.CrawlMoveAcceleration);
    }

    /// <summary>
    /// Avoids the driver being at a large distance from the stroller and being able to control it.
    /// </summary>
    private void CheckRiderDistance(EntityUid vehicleUid, ImperialVehicleComponent vehicleComponent)
    {
        {
            if (vehicleComponent.Rider == null)
                return;

            var riderUid = vehicleComponent.Rider.Value;
            var vehicleXform = Transform(vehicleUid);
            var riderXform = Transform(riderUid);

            var distance = (riderXform.WorldPosition - vehicleXform.WorldPosition).Length();
            var distanceValid = distance <= vehicleComponent.MaxRiderDistance;

            var parentValid = riderXform.ParentUid == vehicleUid;

            var containerValid = !IsInForeignContainer(riderUid, vehicleUid);

            if (!distanceValid || !parentValid || !containerValid)
            {
                ForceUnbuckleRider(vehicleUid, riderUid, vehicleComponent);
            }
        }
    }

    private bool IsInForeignContainer(EntityUid entity, EntityUid vehicle)
    {
        var containerManager = Comp<ContainerManagerComponent>(entity);
        foreach (var container in containerManager.Containers.Values)
        {
            if (container.Contains(entity) && container.Owner != vehicle)
            {
                return true;
            }
        }
        return false;
    }

    private void ForceUnbuckleRider(EntityUid vehicleUid, EntityUid riderUid, ImperialVehicleComponent vehicleComponent)
    {
        if (TryComp<InputMoverComponent>(riderUid, out var inputMover))
        {
            inputMover.CanMove = true;
            Dirty(riderUid, inputMover);
        }

        _buckleSystem.TryUnbuckle(riderUid, riderUid);

        var riderXform = Transform(riderUid);
        var vehicleXform = Transform(vehicleUid);

        riderXform.AttachToGridOrMap();

        _transform.SetWorldPosition(riderUid, vehicleXform.WorldPosition);
        _transform.SetWorldRotation(riderUid, vehicleXform.WorldRotation);

        RemComp<RelayInputMoverComponent>(riderUid);
        RemComp<ImperialVehiclePilotComponent>(riderUid);
        _tagSystem.RemoveTag(vehicleUid, DoorBumpOpenerTag);

        if (vehicleComponent.HornAction != null)
            _actions.RemoveAction(riderUid, vehicleComponent.HornAction);

        Appearance.SetData(vehicleUid, VehicleVisuals.HideRider, false);
        vehicleComponent.Rider = null;
        Dirty(vehicleUid, vehicleComponent);

        _popup.PopupEntity(Loc.GetString("imperial-vehicle-distance-unbuckle"), riderUid, riderUid);
    }

}

