using Content.Shared.Actions;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.DoAfter;
using Content.Shared.Imperial.XxRaay.Components;
using Content.Shared.Imperial.XxRaay.Events;
using Content.Shared.Interaction.Events;
using Content.Shared.Popups;

namespace Content.Shared.Imperial.XxRaay.Systems;

public abstract class SharedWormEvolutionSystem : EntitySystem
{
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedWormBloodDrinkSystem _wormBloodDrink = default!;
    [Dependency] private readonly SharedWormCorpsePossessionSystem _wormCorpsePossession = default!;
    [Dependency] private readonly SharedWormDoorHideSystem _wormDoorHide = default!;

    private EntityQuery<ActiveVentCrawlingComponent> _ventCrawlingQuery;
    private EntityQuery<ActiveWormDoorHidingComponent> _doorHidingQuery;
    private EntityQuery<WormBloodDrinkingComponent> _drinkingQuery;
    private EntityQuery<ActiveWormCorpsePossessionComponent> _possessionQuery;
    private EntityQuery<ActiveWormEvolutionComponent> _evolutionQuery;
    private EntityQuery<ActiveWormReproductionComponent> _reproductionQuery;
    private EntityQuery<WormBloodComponent> _bloodQuery;

    public override void Initialize()
    {
        base.Initialize();

        _ventCrawlingQuery = GetEntityQuery<ActiveVentCrawlingComponent>();
        _doorHidingQuery = GetEntityQuery<ActiveWormDoorHidingComponent>();
        _drinkingQuery = GetEntityQuery<WormBloodDrinkingComponent>();
        _possessionQuery = GetEntityQuery<ActiveWormCorpsePossessionComponent>();
        _evolutionQuery = GetEntityQuery<ActiveWormEvolutionComponent>();
        _reproductionQuery = GetEntityQuery<ActiveWormReproductionComponent>();
        _bloodQuery = GetEntityQuery<WormBloodComponent>();

        SubscribeLocalEvent<WormEvolverComponent, WormEvolutionActionEvent>(OnEvolutionAction);
        SubscribeLocalEvent<WormEvolverComponent, WormEvolutionDoAfterEvent>(OnEvolutionDoAfter);

        SubscribeLocalEvent<ActiveWormEvolutionComponent, ComponentStartup>(OnEvolutionStartup);
        SubscribeLocalEvent<ActiveWormEvolutionComponent, BeforeDamageChangedEvent>(OnBeforeDamage);
        SubscribeLocalEvent<ActiveWormEvolutionComponent, InteractionAttemptEvent>(OnInteractionAttempt);
        SubscribeLocalEvent<ActiveWormEvolutionComponent, UseAttemptEvent>(OnUseAttempt);
        SubscribeLocalEvent<ActiveWormEvolutionComponent, AttackAttemptEvent>(OnAttackAttempt);
    }

    private void OnEvolutionAction(Entity<WormEvolverComponent> ent, ref WormEvolutionActionEvent args)
    {
        if (args.Handled)
            return;

        if (!CanEvolve(ent, showPopup: true, popupUser: args.Performer))
            return;

        args.Handled = true;

        _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager, ent, ent.Comp.WeaveDelay,
                new WormEvolutionDoAfterEvent(), ent)
            {
                BreakOnMove = true,
                BreakOnWeightlessMove = true,
                BreakOnDamage = true,
                NeedHand = false,
            });

        _popup.PopupPredictedCoordinates(
            Loc.GetString("worm-evolution-weaving"),
            Transform(ent).Coordinates,
            args.Performer,
            PopupType.Medium);
    }

    private void OnEvolutionDoAfter(Entity<WormEvolverComponent> ent, ref WormEvolutionDoAfterEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        if (args.Cancelled)
            return;

        if (!CanEvolve(ent))
            return;

        BeginEvolution(ent.Owner, ent.Comp);
    }

    private void OnEvolutionStartup(Entity<ActiveWormEvolutionComponent> ent, ref ComponentStartup args)
    {
        _wormBloodDrink.StopDrinking(ent.Owner);
        _wormCorpsePossession.ForceExit(ent.Owner, forced: true);
        _wormDoorHide.ForceExit(ent.Owner);
        OnEvolutionStarted(ent.Owner);
    }

    private void OnBeforeDamage(Entity<ActiveWormEvolutionComponent> ent, ref BeforeDamageChangedEvent args)
    {
        args.Cancelled = true;
    }

    private void OnInteractionAttempt(Entity<ActiveWormEvolutionComponent> ent, ref InteractionAttemptEvent args)
    {
        args.Cancelled = true;
    }

    private void OnUseAttempt(Entity<ActiveWormEvolutionComponent> ent, ref UseAttemptEvent args)
    {
        args.Cancel();
    }

    private void OnAttackAttempt(Entity<ActiveWormEvolutionComponent> ent, ref AttackAttemptEvent args)
    {
        args.Cancel();
    }

    public bool IsEvolving(EntityUid worm) => _evolutionQuery.HasComp(worm);

    public bool CanEvolve(Entity<WormEvolverComponent> worm, bool showPopup = false, EntityUid? popupUser = null)
    {
        if (_evolutionQuery.HasComp(worm))
        {
            if (showPopup)
                ShowFailPopup(worm.Owner, Loc.GetString("worm-evolution-fail-evolving"), popupUser);
            return false;
        }

        if (_reproductionQuery.HasComp(worm))
        {
            if (showPopup)
                ShowFailPopup(worm.Owner, Loc.GetString("worm-evolution-fail-reproducing"), popupUser);
            return false;
        }

        if (_ventCrawlingQuery.HasComp(worm))
        {
            if (showPopup)
                ShowFailPopup(worm.Owner, Loc.GetString("worm-evolution-fail-vent"), popupUser);
            return false;
        }

        if (_doorHidingQuery.HasComp(worm))
        {
            if (showPopup)
                ShowFailPopup(worm.Owner, Loc.GetString("worm-evolution-fail-door"), popupUser);
            return false;
        }

        if (_possessionQuery.HasComp(worm))
        {
            if (showPopup)
                ShowFailPopup(worm.Owner, Loc.GetString("worm-evolution-fail-corpse"), popupUser);
            return false;
        }

        if (_drinkingQuery.HasComp(worm))
        {
            if (showPopup)
                ShowFailPopup(worm.Owner, Loc.GetString("worm-evolution-fail-drinking"), popupUser);
            return false;
        }

        if (!_bloodQuery.TryComp(worm, out var blood) || blood.Blood < worm.Comp.BloodCost)
        {
            if (showPopup)
                ShowFailPopup(worm.Owner, Loc.GetString("worm-evolution-fail-blood", ("cost", worm.Comp.BloodCost)), popupUser);
            return false;
        }

        return true;
    }

    protected virtual void OnEvolutionStarted(EntityUid worm)
    {
    }

    protected virtual void BeginEvolution(EntityUid worm, WormEvolverComponent evolver)
    {
    }

    private void ShowFailPopup(EntityUid worm, string message, EntityUid? popupUser)
    {
        _popup.PopupPredicted(message, worm, popupUser, PopupType.Small);
    }
}


