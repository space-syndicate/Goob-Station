using Content.Shared.Actions;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.DoAfter;
using Content.Shared.Imperial.XxRaay.Components;
using Content.Shared.Imperial.XxRaay.Events;
using Content.Shared.Interaction.Events;
using Content.Shared.Popups;
using Robust.Shared.Network;
using Robust.Shared.Player;

namespace Content.Shared.Imperial.XxRaay.Systems;

public abstract class SharedWormReproductionSystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly ISharedPlayerManager _player = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedWormBloodDrinkSystem _wormBloodDrink = default!;
    [Dependency] private readonly SharedWormCorpsePossessionSystem _wormCorpsePossession = default!;
    [Dependency] private readonly SharedWormDoorHideSystem _wormDoorHide = default!;

    private EntityQuery<ActiveVentCrawlingComponent> _ventCrawlingQuery;
    private EntityQuery<ActiveWormDoorHidingComponent> _doorHidingQuery;
    private EntityQuery<WormBloodDrinkingComponent> _drinkingQuery;
    private EntityQuery<ActiveWormCorpsePossessionComponent> _possessionQuery;
    private EntityQuery<ActiveWormReproductionComponent> _reproductionQuery;
    private EntityQuery<ActiveWormEvolutionComponent> _evolutionQuery;
    private EntityQuery<WormBloodComponent> _bloodQuery;

    public override void Initialize()
    {
        base.Initialize();

        _ventCrawlingQuery = GetEntityQuery<ActiveVentCrawlingComponent>();
        _doorHidingQuery = GetEntityQuery<ActiveWormDoorHidingComponent>();
        _drinkingQuery = GetEntityQuery<WormBloodDrinkingComponent>();
        _possessionQuery = GetEntityQuery<ActiveWormCorpsePossessionComponent>();
        _reproductionQuery = GetEntityQuery<ActiveWormReproductionComponent>();
        _evolutionQuery = GetEntityQuery<ActiveWormEvolutionComponent>();
        _bloodQuery = GetEntityQuery<WormBloodComponent>();

        SubscribeLocalEvent<WormReproducerComponent, MapInitEvent>(OnReproducerMapInit);
        SubscribeLocalEvent<WormReproducerComponent, ComponentShutdown>(OnReproducerShutdown);
        SubscribeLocalEvent<WormReproducerComponent, WormReproductionActionEvent>(OnReproductionAction);
        SubscribeLocalEvent<WormReproducerComponent, WormReproductionDoAfterEvent>(OnReproductionDoAfter);

        SubscribeLocalEvent<ActiveWormReproductionComponent, ComponentStartup>(OnReproductionStartup);
        SubscribeLocalEvent<ActiveWormReproductionComponent, BeforeDamageChangedEvent>(OnBeforeDamage);
        SubscribeLocalEvent<ActiveWormReproductionComponent, InteractionAttemptEvent>(OnInteractionAttempt);
        SubscribeLocalEvent<ActiveWormReproductionComponent, UseAttemptEvent>(OnUseAttempt);
        SubscribeLocalEvent<ActiveWormReproductionComponent, AttackAttemptEvent>(OnAttackAttempt);
    }

    private void OnReproducerMapInit(Entity<WormReproducerComponent> ent, ref MapInitEvent args)
    {
        if (_net.IsServer)
            _actions.AddAction(ent, ref ent.Comp.ReproductionActionEntity, ent.Comp.ReproductionAction);
    }

    private void OnReproducerShutdown(Entity<WormReproducerComponent> ent, ref ComponentShutdown args)
    {
        if (_net.IsServer && ent.Comp.ReproductionActionEntity != null)
            _actions.RemoveAction(ent.Owner, ent.Comp.ReproductionActionEntity);
    }

    private void OnReproductionAction(Entity<WormReproducerComponent> ent, ref WormReproductionActionEvent args)
    {
        if (args.Handled)
            return;

        if (!CanReproduce(ent, showPopup: true, popupUser: args.Performer))
            return;

        args.Handled = true;

        if (!_doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager, ent, TimeSpan.FromSeconds(ent.Comp.WeaveDelay),
                new WormReproductionDoAfterEvent(), ent)
            {
                BreakOnMove = true,
                BreakOnWeightlessMove = true,
                BreakOnDamage = true,
                NeedHand = false,
            }))
            return;

        if (_net.IsServer)
        {
            _popup.PopupCoordinates(
                Loc.GetString("worm-reproduction-weaving"),
                Transform(ent).Coordinates,
                PopupType.Medium);
        }
    }

    private void OnReproductionDoAfter(Entity<WormReproducerComponent> ent, ref WormReproductionDoAfterEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        if (args.Cancelled)
            return;

        if (!CanReproduce(ent))
            return;

        if (!_net.IsServer)
            return;

        BeginReproduction(ent.Owner, ent.Comp);
    }

    private void OnReproductionStartup(Entity<ActiveWormReproductionComponent> ent, ref ComponentStartup args)
    {
        _wormBloodDrink.StopDrinking(ent.Owner);
        _wormCorpsePossession.ForceExit(ent.Owner, forced: true);
        _wormDoorHide.ForceExit(ent.Owner);
        OnReproductionStarted(ent.Owner);
    }

    private void OnBeforeDamage(Entity<ActiveWormReproductionComponent> ent, ref BeforeDamageChangedEvent args)
    {
        args.Cancelled = true;
    }

    private void OnInteractionAttempt(Entity<ActiveWormReproductionComponent> ent, ref InteractionAttemptEvent args)
    {
        args.Cancelled = true;
    }

    private void OnUseAttempt(Entity<ActiveWormReproductionComponent> ent, ref UseAttemptEvent args)
    {
        args.Cancel();
    }

    private void OnAttackAttempt(Entity<ActiveWormReproductionComponent> ent, ref AttackAttemptEvent args)
    {
        args.Cancel();
    }

    public bool IsReproducing(EntityUid worm) => _reproductionQuery.HasComp(worm);

    public bool CanReproduce(Entity<WormReproducerComponent> worm, bool showPopup = false, EntityUid? popupUser = null)
    {
        if (_reproductionQuery.HasComp(worm))
        {
            if (showPopup)
                ShowFailPopup(worm.Owner, Loc.GetString("worm-reproduction-fail-reproducing"), popupUser);
            return false;
        }

        if (_evolutionQuery.HasComp(worm))
        {
            if (showPopup)
                ShowFailPopup(worm.Owner, Loc.GetString("worm-reproduction-fail-evolving"), popupUser);
            return false;
        }

        if (_ventCrawlingQuery.HasComp(worm))
        {
            if (showPopup)
                ShowFailPopup(worm.Owner, Loc.GetString("worm-reproduction-fail-vent"), popupUser);
            return false;
        }

        if (_doorHidingQuery.HasComp(worm))
        {
            if (showPopup)
                ShowFailPopup(worm.Owner, Loc.GetString("worm-reproduction-fail-door"), popupUser);
            return false;
        }

        if (_possessionQuery.HasComp(worm))
        {
            if (showPopup)
                ShowFailPopup(worm.Owner, Loc.GetString("worm-reproduction-fail-corpse"), popupUser);
            return false;
        }

        if (_drinkingQuery.HasComp(worm))
        {
            if (showPopup)
                ShowFailPopup(worm.Owner, Loc.GetString("worm-reproduction-fail-drinking"), popupUser);
            return false;
        }

        if (!_bloodQuery.TryComp(worm, out var blood) || blood.Blood < worm.Comp.BloodCost)
        {
            if (showPopup)
                ShowFailPopup(worm.Owner, Loc.GetString("worm-reproduction-fail-blood"), popupUser);
            return false;
        }

        return true;
    }

    protected virtual void OnReproductionStarted(EntityUid worm)
    {
    }

    protected abstract void BeginReproduction(EntityUid worm, WormReproducerComponent reproducer);

    private void ShowFailPopup(EntityUid worm, string message, EntityUid? popupUser)
    {
        if (_net.IsClient)
        {
            _popup.PopupClient(message, worm, popupUser);
            return;
        }

        _popup.PopupEntity(message, worm, PopupType.Small);

        if (popupUser != null && _player.TryGetSessionByEntity(popupUser.Value, out var session))
            _popup.PopupEntity(message, worm, session, PopupType.Small);
    }
}
