using Content.Server.Imperial.XxRaay.Components;
using Content.Server.Stealth;
using Content.Shared.ActionBlocker;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Doors;
using Content.Shared.Doors.Components;
using Content.Shared.Doors.Systems;
using Content.Shared.DoAfter;
using Content.Shared.Imperial.XxRaay.Components;
using Content.Shared.Imperial.XxRaay.Events;
using Content.Shared.Imperial.XxRaay.Systems;
using Content.Shared.Interaction;
using Content.Shared.Movement.Events;
using Content.Shared.Popups;
using Content.Shared.Stealth;
using Content.Shared.Stealth.Components;
using Content.Shared.Stunnable;
using Content.Shared.Tools.Systems;
using Content.Shared.Verbs;
using Content.Shared.Eye;
using Content.Shared.Tag;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Prototypes;

namespace Content.Server.Imperial.XxRaay.Systems;

public sealed class WormDoorHideSystem : SharedWormDoorHideSystem
{
    [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedEyeSystem _eye = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedDoorSystem _doorSystem = default!;
    [Dependency] private readonly SharedStaminaSystem _stamina = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly WeldableSystem _weldable = default!;
    [Dependency] private readonly StealthSystem _stealth = default!;
    [Dependency] private readonly VisibilitySystem _visibility = default!;
    [Dependency] private readonly ImperialVentCrawlerSystem _ventCrawler = default!;
    [Dependency] private readonly TagSystem _tags = default!;

    private EntityQuery<ActiveVentCrawlingComponent> _ventCrawlingQuery;
    private EntityQuery<ActiveWormCorpsePossessionComponent> _corpsePossessionQuery;
    private EntityQuery<ActiveWormDoorHidingComponent> _doorHidingQuery;
    private EntityQuery<WormBloodDrinkingComponent> _drinkingQuery;
    private EntityQuery<WormDoorHideOccupiedComponent> _occupiedQuery;
    private EntityQuery<ActiveWormEvolutionComponent> _evolutionQuery;
    private EntityQuery<ActiveWormReproductionComponent> _reproductionQuery;

    public override void Initialize()
    {
        base.Initialize();

        _ventCrawlingQuery = GetEntityQuery<ActiveVentCrawlingComponent>();
        _corpsePossessionQuery = GetEntityQuery<ActiveWormCorpsePossessionComponent>();
        _doorHidingQuery = GetEntityQuery<ActiveWormDoorHidingComponent>();
        _drinkingQuery = GetEntityQuery<WormBloodDrinkingComponent>();
        _occupiedQuery = GetEntityQuery<WormDoorHideOccupiedComponent>();
        _evolutionQuery = GetEntityQuery<ActiveWormEvolutionComponent>();
        _reproductionQuery = GetEntityQuery<ActiveWormReproductionComponent>();

        SubscribeLocalEvent<AirlockComponent, GetVerbsEvent<InteractionVerb>>(OnAirlockGetVerbs);
        SubscribeLocalEvent<AirlockComponent, WormDoorHideDoAfterEvent>(OnHideDoAfter);
        SubscribeLocalEvent<WormDoorHidingComponent, MoveEvent>(OnMoved);
        SubscribeLocalEvent<WormDoorHidingComponent, ComponentShutdown>(OnHidingShutdown);
        SubscribeLocalEvent<WormDoorHideOccupiedComponent, ComponentShutdown>(OnOccupiedShutdown);
        SubscribeLocalEvent<WormDoorHideOccupiedComponent, BeforeDoorOpenedEvent>(OnBeforeDoorOpened);
        SubscribeLocalEvent<WormDoorHideOccupiedComponent, DoorStateChangedEvent>(OnDoorStateChanged);
    }

    public override void ForceExit(EntityUid worm)
    {
        if (!_doorHidingQuery.HasComp(worm))
            return;

        ExitDoorHide(worm);
    }

    private void OnAirlockGetVerbs(Entity<AirlockComponent> ent, ref GetVerbsEvent<InteractionVerb> args)
    {
        if (!_actionBlocker.CanConsciouslyPerformAction(args.User) || !_interaction.InRangeUnobstructed(args.User, ent.Owner))
            return;

        if (!TryComp(args.User, out WormDoorHiderComponent? hider))
            return;

        if (!TryComp(ent, out DoorComponent? door))
            return;

        var user = args.User;

        if (CanHideInDoor(user, ent, door, hider))
        {
            args.Verbs.Add(new InteractionVerb
            {
                Priority = hider.EnterVerbPriority,
                Text = Loc.GetString(hider.EnterVerb),
                Icon = hider.EnterVerbIcon,
                DoContactInteraction = false,
                Act = () => StartHideInDoor(user, ent, hider),
            });
        }

        if (TryComp(user, out WormDoorHidingComponent? hiding) && CanExitDoor(user, ent, hiding))
        {
            args.Verbs.Add(new InteractionVerb
            {
                Priority = hider.ExitVerbPriority,
                Text = Loc.GetString(hider.ExitVerb),
                Icon = hider.ExitVerbIcon,
                DoContactInteraction = false,
                Act = () => ExitDoorHide(user),
            });
        }
    }

    private void StartHideInDoor(EntityUid worm, EntityUid door, WormDoorHiderComponent hider)
    {
        if (!TryComp(door, out DoorComponent? doorComp) || !CanHideInDoor(worm, door, doorComp, hider, showPopup: true))
            return;

        if (!_actionBlocker.CanConsciouslyPerformAction(worm) || !_interaction.InRangeUnobstructed(worm, door))
            return;

        var args = new DoAfterArgs(EntityManager, worm, hider.EnterDelay, new WormDoorHideDoAfterEvent(), door, target: door)
        {
            BreakOnMove = true,
            BreakOnWeightlessMove = true,
            BreakOnDamage = true,
            NeedHand = false,
        };

        _doAfter.TryStartDoAfter(args);
    }

    private void OnHideDoAfter(Entity<AirlockComponent> ent, ref WormDoorHideDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        args.Handled = true;

        if (!TryComp(args.User, out WormDoorHiderComponent? hider))
            return;

        if (!TryComp(ent, out DoorComponent? door))
            return;

        if (!CanHideInDoor(args.User, ent, door, hider))
            return;

        if (!_actionBlocker.CanConsciouslyPerformAction(args.User))
            return;

        EnterDoorHide(args.User, ent, hider);
    }

    private void OnMoved(Entity<WormDoorHidingComponent> ent, ref MoveEvent args)
    {
        if (ent.Comp.IgnoreNextMove)
        {
            ent.Comp.IgnoreNextMove = false;
            return;
        }

        ExitDoorHide(ent.Owner);
    }

    private void OnHidingShutdown(Entity<WormDoorHidingComponent> ent, ref ComponentShutdown args)
    {
        RestorePhysics(ent.Owner, ent.Comp);
        RestoreStealth(ent.Owner, ent.Comp);
        if (ent.Comp.AppliedVisibility)
            RestoreVisibility(ent.Owner, ent.Comp);

        if (TryComp(ent.Owner, out EyeComponent? eye))
            _eye.RefreshVisibilityMask((ent.Owner, eye));

        RemComp<ActiveWormDoorHidingComponent>(ent.Owner);
    }

    private void OnOccupiedShutdown(Entity<WormDoorHideOccupiedComponent> ent, ref ComponentShutdown args)
    {
        if (!Exists(ent.Comp.Worm) || !TryComp(ent.Comp.Worm, out WormDoorHidingComponent? hiding))
            return;

        if (hiding.Exiting)
            return;

        RemComp<WormDoorHidingComponent>(ent.Comp.Worm);
    }

    private void OnBeforeDoorOpened(Entity<WormDoorHideOccupiedComponent> ent, ref BeforeDoorOpenedEvent args)
    {
        if (args.User is not { } user || user == ent.Comp.Worm)
            return;

        if (!TryComp(ent.Comp.Worm, out WormDoorHiderComponent? hider) || !hider.AmbushOnOpen)
            return;

        if (!TryComp(ent, out DoorComponent? door) || !_doorSystem.HasAccess(ent, user, door))
            return;

        if (!_actionBlocker.CanConsciouslyPerformAction(user) || !_interaction.InRangeUnobstructed(user, ent.Owner))
            return;

        AmbushDoorOpener(ent.Comp.Worm, user, hider);
    }

    private void OnDoorStateChanged(Entity<WormDoorHideOccupiedComponent> ent, ref DoorStateChangedEvent args)
    {
        if (args.State is DoorState.Open or DoorState.Opening)
            ExitDoorHide(ent.Comp.Worm);
    }

    private void EnterDoorHide(EntityUid worm, EntityUid door, WormDoorHiderComponent hider)
    {
        if (_doorHidingQuery.HasComp(worm))
            return;

        _ventCrawler.ForceExit(worm);

        var hiding = AddComp<WormDoorHidingComponent>(worm);
        hiding.SourceDoor = door;

        if (TryComp(worm, out PhysicsComponent? physics))
        {
            hiding.WasCollidable = physics.CanCollide;

            if (physics.CanCollide)
                _physics.SetCanCollide(worm, false, body: physics);

            _physics.ResetDynamics(worm, physics);
        }

        if (hider.UseStealth)
            ApplyStealth(worm, hiding, hider);

        ApplyDoorVisibility(worm, hiding, hider);

        var occupied = EnsureComp<WormDoorHideOccupiedComponent>(door);
        occupied.Worm = worm;
        Dirty(door, occupied);

        hiding.IgnoreNextMove = true;
        EnsureComp<ActiveWormDoorHidingComponent>(worm);
        _transform.SetCoordinates(worm, Transform(worm), Transform(door).Coordinates);

        if (TryComp(worm, out EyeComponent? eye))
            _eye.RefreshVisibilityMask((worm, eye));
    }

    private void AmbushDoorOpener(EntityUid worm, EntityUid target, WormDoorHiderComponent hider)
    {
        ExitDoorHide(worm);

        if (!Exists(worm) || !Exists(target))
            return;

        _transform.SetCoordinates(worm, Transform(worm), Transform(target).Coordinates);
        _stun.TryKnockdown(target, hider.AmbushKnockdownTime, force: true);

        if (TryComp(target, out StaminaComponent? stamina))
        {
            var remainingStamina = MathF.Max(0f, stamina.CritThreshold - _stamina.GetStaminaDamage(target, stamina));
            var staminaDamage = MathF.Max(hider.AmbushStaminaDamage, remainingStamina);
            _stamina.TakeStaminaDamage(target, staminaDamage, stamina, source: worm, visual: true, ignoreResist: true);
        }

        _popup.PopupEntity(Loc.GetString("worm-door-hide-ambush"), target, PopupType.LargeCaution);
    }

    private void ExitDoorHide(EntityUid worm)
    {
        if (!TryComp(worm, out WormDoorHidingComponent? hiding) || hiding.Exiting)
            return;

        hiding.Exiting = true;
        var door = hiding.SourceDoor;

        if (Exists(door) && TryComp(door, out WormDoorHideOccupiedComponent? occupied) && occupied.Worm == worm)
            RemComp<WormDoorHideOccupiedComponent>(door);

        RemComp<WormDoorHidingComponent>(worm);
    }

    private bool CanHideInDoor(
        EntityUid worm,
        EntityUid door,
        DoorComponent doorComp,
        WormDoorHiderComponent hider,
        bool showPopup = false)
    {
        if (_doorHidingQuery.HasComp(worm))
            return false;

        if (_ventCrawlingQuery.HasComp(worm))
        {
            if (showPopup)
                ShowFailPopup(worm, Loc.GetString("worm-door-hide-fail-vent"));
            return false;
        }

        if (_corpsePossessionQuery.HasComp(worm))
        {
            if (showPopup)
                ShowFailPopup(worm, Loc.GetString("worm-door-hide-fail-possessing"));
            return false;
        }

        if (_drinkingQuery.HasComp(worm))
        {
            if (showPopup)
                ShowFailPopup(worm, Loc.GetString("worm-door-hide-fail-drinking"));
            return false;
        }

        if (_evolutionQuery.HasComp(worm))
        {
            if (showPopup)
                ShowFailPopup(worm, Loc.GetString("worm-door-hide-fail-evolving"));
            return false;
        }

        if (_reproductionQuery.HasComp(worm))
        {
            if (showPopup)
                ShowFailPopup(worm, Loc.GetString("worm-door-hide-fail-reproducing"));
            return false;
        }

        if (_occupiedQuery.HasComp(door))
        {
            if (showPopup)
                ShowFailPopup(worm, Loc.GetString("worm-door-hide-fail-occupied"));
            return false;
        }

        if (!Transform(door).Anchored)
            return false;

        if (doorComp.State == DoorState.Welded || _weldable.IsWelded(door))
        {
            if (showPopup)
                ShowFailPopup(worm, Loc.GetString("worm-door-hide-fail-welded"));
            return false;
        }

        if (doorComp.State != DoorState.Closed)
        {
            if (showPopup)
                ShowFailPopup(worm, Loc.GetString("worm-door-hide-fail-open"));
            return false;
        }

        if (_tags.HasTag(door, hider.HighSecDoorTag))
        {
            if (showPopup)
                ShowFailPopup(worm, Loc.GetString("worm-door-hide-fail-armored"));
            return false;
        }

        return _interaction.InRangeUnobstructed(worm, door, popup: showPopup);
    }

    private bool CanExitDoor(EntityUid worm, EntityUid door, WormDoorHidingComponent hiding)
    {
        return hiding.SourceDoor == door && IsOnSameTile(worm, door);
    }

    private bool IsOnSameTile(EntityUid first, EntityUid second)
    {
        if (!TryGetGridTile(Transform(first).Coordinates, out var firstGrid, out _, out var firstTile))
            return false;

        if (!TryGetGridTile(Transform(second).Coordinates, out var secondGrid, out _, out var secondTile))
            return false;

        return firstGrid == secondGrid && firstTile == secondTile;
    }

    private bool TryGetGridTile(EntityCoordinates coordinates, out EntityUid gridUid, out MapGridComponent grid, out Vector2i tile)
    {
        gridUid = default;
        grid = default!;
        tile = default;

        if (!TryComp(coordinates.EntityId, out MapGridComponent? gridComp))
            return false;

        grid = gridComp;
        gridUid = coordinates.EntityId;
        tile = _map.TileIndicesFor(gridUid, grid, coordinates);
        return true;
    }

    private void ApplyDoorVisibility(EntityUid worm, WormDoorHidingComponent hiding, WormDoorHiderComponent hider)
    {
        hiding.AppliedVisibility = true;

        var hadVisibility = TryComp(worm, out VisibilityComponent? visibility);
        if (!hadVisibility)
        {
            visibility = EnsureComp<VisibilityComponent>(worm);
            hiding.AddedVisibility = true;
        }

        hiding.PreviousVisibilityLayer = visibility!.Layer;
        _visibility.AddLayer((worm, visibility), hider.DoorHideVisibilityLayer, false);
        _visibility.RemoveLayer((worm, visibility), hider.HiddenVisibilityLayer, false);
        _visibility.RefreshVisibility(worm, visibilityComponent: visibility);
    }

    private void RestoreVisibility(EntityUid worm, WormDoorHidingComponent hiding)
    {
        if (!TryComp(worm, out VisibilityComponent? visibility))
            return;

        if (hiding.AddedVisibility)
        {
            RemComp<VisibilityComponent>(worm);
            return;
        }

        _visibility.SetLayer((worm, visibility), hiding.PreviousVisibilityLayer, false);
        _visibility.RefreshVisibility(worm, visibilityComponent: visibility);
    }

    private void ApplyStealth(EntityUid worm, WormDoorHidingComponent hiding, WormDoorHiderComponent hider)
    {
        var hadStealth = TryComp(worm, out StealthComponent? stealth);
        if (!hadStealth)
        {
            stealth = EnsureComp<StealthComponent>(worm);
            hiding.AddedStealth = true;
        }

        hiding.PreviousStealthEnabled = stealth!.Enabled;
        hiding.PreviousStealthVisibility = _stealth.GetVisibility(worm, stealth);

        _stealth.SetEnabled(worm, true, stealth);
        _stealth.SetVisibility(worm, hider.StealthVisibility, stealth);
    }

    private void RestoreStealth(EntityUid worm, WormDoorHidingComponent hiding)
    {
        if (!TryComp(worm, out StealthComponent? stealth))
            return;

        if (hiding.AddedStealth)
        {
            RemComp<StealthComponent>(worm);
            return;
        }

        _stealth.SetEnabled(worm, hiding.PreviousStealthEnabled, stealth);
        _stealth.SetVisibility(worm, hiding.PreviousStealthVisibility, stealth);
    }

    private void RestorePhysics(EntityUid worm, WormDoorHidingComponent hiding)
    {
        if (!TryComp(worm, out PhysicsComponent? physics))
            return;

        _physics.SetCanCollide(worm, hiding.WasCollidable, body: physics);
        _physics.ResetDynamics(worm, physics);
    }

    private void ShowFailPopup(EntityUid worm, string message)
    {
        _popup.PopupEntity(message, worm, worm, PopupType.Small);
    }
}
