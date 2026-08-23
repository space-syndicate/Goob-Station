using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.DoAfter;
using Content.Shared.Imperial.XxRaay.Components;
using Content.Shared.Imperial.XxRaay.Events;
using Content.Shared.Interaction;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Shared.Imperial.XxRaay.Systems;

public abstract class SharedWormCorpsePossessionSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly ISharedPlayerManager _player = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    private EntityQuery<ActiveVentCrawlingComponent> _ventCrawlingQuery;
    private EntityQuery<ActiveWormDoorHidingComponent> _doorHidingQuery;
    private EntityQuery<WormBloodDrinkingComponent> _drinkingQuery;
    private EntityQuery<ActiveWormCorpsePossessionComponent> _possessionQuery;
    private EntityQuery<ActiveWormEvolutionComponent> _evolutionQuery;
    private EntityQuery<ActiveWormReproductionComponent> _reproductionQuery;
    private EntityQuery<WormCorpseOccupiedComponent> _occupiedQuery;
    private EntityQuery<WormBloodComponent> _wormBloodQuery;

    public override void Initialize()
    {
        base.Initialize();

        _ventCrawlingQuery = GetEntityQuery<ActiveVentCrawlingComponent>();
        _doorHidingQuery = GetEntityQuery<ActiveWormDoorHidingComponent>();
        _drinkingQuery = GetEntityQuery<WormBloodDrinkingComponent>();
        _possessionQuery = GetEntityQuery<ActiveWormCorpsePossessionComponent>();
        _evolutionQuery = GetEntityQuery<ActiveWormEvolutionComponent>();
        _reproductionQuery = GetEntityQuery<ActiveWormReproductionComponent>();
        _occupiedQuery = GetEntityQuery<WormCorpseOccupiedComponent>();
        _wormBloodQuery = GetEntityQuery<WormBloodComponent>();

        SubscribeLocalEvent<WormCorpseHostComponent, ComponentShutdown>(OnHostShutdown);
        SubscribeLocalEvent<WormCorpseHostComponent, WormCorpseEnterActionEvent>(OnEnterAction);
        SubscribeLocalEvent<WormCorpseHostComponent, WormCorpseEnterDoAfterEvent>(OnEnterDoAfter);
        SubscribeLocalEvent<WormCorpseOccupiedComponent, WormCorpseExitActionEvent>(OnExitAction);
    }

    private void OnHostShutdown(Entity<WormCorpseHostComponent> ent, ref ComponentShutdown args)
    {
        if (_possessionQuery.HasComp(ent.Owner))
            ForceExit(ent.Owner, forced: true);

        OnHostShutdown(ent);
    }

    protected virtual void OnHostShutdown(Entity<WormCorpseHostComponent> ent)
    {
    }

    private void OnEnterAction(Entity<WormCorpseHostComponent> ent, ref WormCorpseEnterActionEvent args)
    {
        if (args.Handled)
            return;

        var target = args.Target;

        if (!CanPossess(ent, target, showPopup: true, popupUser: args.Performer))
            return;

        args.Handled = true;

        _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager, ent, ent.Comp.EnterDelay,
                new WormCorpseEnterDoAfterEvent(), ent, target: target)
            {
                BreakOnMove = true,
                BreakOnWeightlessMove = true,
                BreakOnDamage = true,
                NeedHand = false,
                DistanceThreshold = ent.Comp.Range,
            });
    }

    private void OnEnterDoAfter(Entity<WormCorpseHostComponent> ent, ref WormCorpseEnterDoAfterEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        if (args.Cancelled)
            return;

        if (args.Target is not { } target || !CanPossess(ent, target))
            return;

        if (!_net.IsServer)
            return;

        TryPossess(ent.Owner, target, ent.Comp);
    }

    private void OnExitAction(Entity<WormCorpseOccupiedComponent> ent, ref WormCorpseExitActionEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        if (!_net.IsServer)
            return;

        ExitPossession(ent.Comp.Worm, ent.Owner, forced: false);
    }

    public bool CanPossess(Entity<WormCorpseHostComponent> worm, EntityUid target, bool showPopup = false, EntityUid? popupUser = null)
    {
        if (worm.Owner == target)
            return false;

        if (_possessionQuery.HasComp(worm))
        {
            if (showPopup)
                ShowFailPopup(worm.Owner, Loc.GetString("worm-corpse-fail-already-possessing"), popupUser);
            return false;
        }

        if (_ventCrawlingQuery.HasComp(worm))
        {
            if (showPopup)
                ShowFailPopup(worm.Owner, Loc.GetString("worm-corpse-fail-vent"), popupUser);
            return false;
        }

        if (_doorHidingQuery.HasComp(worm))
        {
            if (showPopup)
                ShowFailPopup(worm.Owner, Loc.GetString("worm-corpse-fail-door"), popupUser);
            return false;
        }

        if (_drinkingQuery.HasComp(worm))
        {
            if (showPopup)
                ShowFailPopup(worm.Owner, Loc.GetString("worm-corpse-fail-drinking"), popupUser);
            return false;
        }

        if (_evolutionQuery.HasComp(worm))
        {
            if (showPopup)
                ShowFailPopup(worm.Owner, Loc.GetString("worm-corpse-fail-evolving"), popupUser);
            return false;
        }

        if (_reproductionQuery.HasComp(worm))
        {
            if (showPopup)
                ShowFailPopup(worm.Owner, Loc.GetString("worm-corpse-fail-reproducing"), popupUser);
            return false;
        }

        if (_occupiedQuery.HasComp(target))
        {
            if (showPopup)
                ShowFailPopup(worm.Owner, Loc.GetString("worm-corpse-fail-occupied"), popupUser);
            return false;
        }

        if (_wormBloodQuery.HasComp(target))
        {
            if (showPopup)
                ShowFailPopup(worm.Owner, Loc.GetString("worm-corpse-fail-worm"), popupUser);
            return false;
        }

        if (!HasComp<MobThresholdsComponent>(target))
        {
            if (showPopup)
                ShowFailPopup(worm.Owner, Loc.GetString("worm-corpse-fail-invalid"), popupUser);
            return false;
        }

        if (!_mobState.IsDead(target))
        {
            if (showPopup)
                ShowFailPopup(worm.Owner, Loc.GetString("worm-corpse-fail-not-dead"), popupUser);
            return false;
        }

        if (worm.Comp.EnterActionEntity != null
            && TryComp(worm.Comp.EnterActionEntity, out ActionComponent? action)
            && action.Cooldown is { } cooldown
            && cooldown.End > _timing.CurTime)
        {
            if (showPopup)
                ShowFailPopup(worm.Owner, Loc.GetString("worm-corpse-fail-cooldown"), popupUser);
            return false;
        }

        return _interaction.InRangeUnobstructed(worm.Owner, target, worm.Comp.Range, popup: showPopup);
    }

    public void ForceExit(EntityUid worm, bool forced)
    {
        if (!_possessionQuery.TryComp(worm, out var active))
            return;

        ExitPossession(worm, active.Corpse, forced);
    }

    protected abstract void TryPossess(EntityUid worm, EntityUid corpse, WormCorpseHostComponent host);

    protected abstract void ExitPossession(EntityUid worm, EntityUid corpse, bool forced);

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
