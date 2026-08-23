using Content.Shared.Body.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;
using Content.Shared.Imperial.XxRaay.Components;
using Content.Shared.Imperial.XxRaay.Events;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Movement.Events;
using Content.Shared.Popups;
using Robust.Shared.Network;
using System.Linq;

namespace Content.Shared.Imperial.XxRaay.Systems;

public abstract class SharedWormBloodDrinkSystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solution = default!;

    private EntityQuery<ActiveVentCrawlingComponent> _ventCrawlingQuery;
    private EntityQuery<ActiveWormDoorHidingComponent> _doorHidingQuery;
    private EntityQuery<ActiveWormCorpsePossessionComponent> _corpsePossessionQuery;
    private EntityQuery<ActiveWormEvolutionComponent> _evolutionQuery;
    private EntityQuery<ActiveWormReproductionComponent> _reproductionQuery;
    private EntityQuery<WormBloodDrinkingComponent> _drinkingQuery;

    public override void Initialize()
    {
        base.Initialize();

        _ventCrawlingQuery = GetEntityQuery<ActiveVentCrawlingComponent>();
        _doorHidingQuery = GetEntityQuery<ActiveWormDoorHidingComponent>();
        _corpsePossessionQuery = GetEntityQuery<ActiveWormCorpsePossessionComponent>();
        _evolutionQuery = GetEntityQuery<ActiveWormEvolutionComponent>();
        _reproductionQuery = GetEntityQuery<ActiveWormReproductionComponent>();
        _drinkingQuery = GetEntityQuery<WormBloodDrinkingComponent>();

        SubscribeLocalEvent<WormBloodDrinkerComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<WormBloodDrinkerComponent, WormBloodDrinkActionEvent>(OnDrinkAction);
        SubscribeLocalEvent<WormBloodDrinkerComponent, WormBloodDrinkAttachDoAfterEvent>(OnAttachDoAfter);
        SubscribeLocalEvent<WormBloodDrinkerComponent, WormBloodDrinkTickDoAfterEvent>(OnTickDoAfter);
        SubscribeLocalEvent<WormBloodDrinkingComponent, MoveEvent>(OnMoveWhileDrinking);
    }

    private void OnShutdown(Entity<WormBloodDrinkerComponent> ent, ref ComponentShutdown args)
    {
        StopDrinking(ent.Owner);
        OnDrinkerShutdown(ent);
    }

    protected virtual void OnDrinkerShutdown(Entity<WormBloodDrinkerComponent> ent)
    {
    }

    private void OnDrinkAction(Entity<WormBloodDrinkerComponent> ent, ref WormBloodDrinkActionEvent args)
    {
        if (args.Handled)
            return;

        if (_drinkingQuery.HasComp(ent))
        {
            args.Handled = true;
            StopDrinking(ent.Owner);
            return;
        }

        var target = args.Target;

        if (!CanStartDrinking(ent, target, showPopup: true, popupUser: args.Performer))
            return;

        args.Handled = true;

        if (!_doAfter.TryStartDoAfter(new DoAfterArgs(
                EntityManager,
                ent,
                ent.Comp.InitialDelay,
                new WormBloodDrinkAttachDoAfterEvent(),
                ent,
                target: target)
            {
                BreakOnMove = true,
                BreakOnWeightlessMove = true,
                BreakOnDamage = true,
                NeedHand = false,
                DistanceThreshold = ent.Comp.Range,
            }))
            return;

        ShowAttachPopup(ent.Owner, target);
    }

    private void ShowAttachPopup(EntityUid worm, EntityUid target)
    {
        if (!_net.IsServer)
            return;

        var message = Loc.GetString("worm-blood-drink-attach",
            ("target", Identity.Entity(target, EntityManager)));

        _popup.PopupEntity(message, worm, PopupType.LargeCaution);
    }

    private void OnAttachDoAfter(Entity<WormBloodDrinkerComponent> ent, ref WormBloodDrinkAttachDoAfterEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        if (args.Cancelled)
            return;

        if (args.Target is not { } target || !CanStartDrinking(ent, target))
            return;

        BeginDrinking(ent.Owner, target);
        StartTickDoAfter(ent, target);
    }

    private void OnTickDoAfter(Entity<WormBloodDrinkerComponent> ent, ref WormBloodDrinkTickDoAfterEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        if (args.Cancelled || args.Target is not { } target || TerminatingOrDeleted(target))
        {
            StopDrinking(ent.Owner, cancelDoAfters: false);
            return;
        }

        if (!TryComp(ent, out WormBloodDrinkerComponent? drinker) || !_drinkingQuery.HasComp(ent))
        {
            StopDrinking(ent.Owner, cancelDoAfters: false);
            return;
        }

        if (!IsInDrinkRange(ent.Owner, target, drinker.Range))
        {
            StopDrinking(ent.Owner, cancelDoAfters: false);
            return;
        }

        if (!CanDrainBlood(ent.Owner, target, drinker))
        {
            StopDrinking(ent.Owner, cancelDoAfters: false);
            return;
        }

        DrainBlood(ent.Owner, target, drinker, drinker.DrainAmount);

        if (!CanDrainBlood(ent.Owner, target, drinker))
        {
            StopDrinking(ent.Owner, cancelDoAfters: false);
            return;
        }

        StartTickDoAfter(ent, target);
    }

    private void OnMoveWhileDrinking(Entity<WormBloodDrinkingComponent> ent, ref MoveEvent args)
    {
        if (args.ParentChanged)
            return;

        if (args.NewPosition == args.OldPosition)
            return;

        StopDrinking(ent.Owner);
    }

    private void StartTickDoAfter(Entity<WormBloodDrinkerComponent> ent, EntityUid target)
    {
        _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager, ent, ent.Comp.TickDelay,
            new WormBloodDrinkTickDoAfterEvent(), ent, target: target)
        {
            BreakOnMove = true,
            BreakOnWeightlessMove = true,
            BreakOnDamage = true,
            NeedHand = false,
            RequireCanInteract = false,
            DistanceThreshold = ent.Comp.Range,
        });
    }

    private void BeginDrinking(EntityUid worm, EntityUid target)
    {
        if (_drinkingQuery.HasComp(worm))
            return;

        var drinking = AddComp<WormBloodDrinkingComponent>(worm);
        drinking.Target = GetNetEntity(target);
        Dirty(worm, drinking);
    }

    protected virtual void DrainBlood(EntityUid worm, EntityUid target, WormBloodDrinkerComponent drinker, FixedPoint2 amount)
    {
        if (!TryComp<BloodstreamComponent>(target, out var blood))
            return;

        if (!_solution.ResolveSolution(target, blood.BloodSolutionName, ref blood.BloodSolution, out var solution))
            return;

        var drain = FixedPoint2.Min(amount, solution.Volume);
        if (drain <= 0)
            return;

        if (!TryComp<WormBloodComponent>(worm, out var wormBlood))
            return;

        var space = wormBlood.MaxBlood - wormBlood.Blood;
        if (space <= 0)
            return;

        var maxDrainBySpace = FixedPoint2.New((int) Math.Ceiling(space / drinker.ConversionRatio));
        drain = FixedPoint2.Min(drain, maxDrainBySpace);
        if (drain <= 0)
            return;

        var gained = Math.Max(1, (int) (drain * drinker.ConversionRatio));
        gained = Math.Min(gained, space);
        if (gained <= 0)
            return;

        _solution.SplitSolution(blood.BloodSolution!.Value, drain);
        Dirty(target, blood);
        OnBloodDrained(worm, target, drinker, gained);
    }

    protected virtual void OnBloodDrained(EntityUid worm, EntityUid target, WormBloodDrinkerComponent drinker, int gained)
    {
    }

    public void StopDrinking(EntityUid worm, bool cancelDoAfters = true)
    {
        if (!_drinkingQuery.TryComp(worm, out var drinking))
            return;

        if (drinking.StoppingDrink)
            return;

        drinking.StoppingDrink = true;
        Dirty(worm, drinking);

        try
        {
            if (cancelDoAfters)
                CancelDoAfters(worm);

            RemCompDeferred<WormBloodDrinkingComponent>(worm);
        }
        catch
        {
            if (TryComp(worm, out WormBloodDrinkingComponent? remaining))
            {
                remaining.StoppingDrink = false;
                Dirty(worm, remaining);
            }

            throw;
        }
    }

    public bool CanStartDrinking(Entity<WormBloodDrinkerComponent> worm, EntityUid target, bool showPopup = false, EntityUid? popupUser = null)
    {
        if (worm.Owner == target)
            return false;

        if (_ventCrawlingQuery.HasComp(worm))
        {
            if (showPopup)
                ShowFailPopup(worm.Owner, Loc.GetString("worm-blood-drink-fail-vent"), popupUser);
            return false;
        }

        if (_doorHidingQuery.HasComp(worm))
        {
            if (showPopup)
                ShowFailPopup(worm.Owner, Loc.GetString("worm-blood-drink-fail-door"), popupUser);
            return false;
        }

        if (_corpsePossessionQuery.HasComp(worm))
        {
            if (showPopup)
                ShowFailPopup(worm.Owner, Loc.GetString("worm-blood-drink-fail-corpse"), popupUser);
            return false;
        }

        if (_evolutionQuery.HasComp(worm))
        {
            if (showPopup)
                ShowFailPopup(worm.Owner, Loc.GetString("worm-blood-drink-fail-evolving"), popupUser);
            return false;
        }

        if (_reproductionQuery.HasComp(worm))
        {
            if (showPopup)
                ShowFailPopup(worm.Owner, Loc.GetString("worm-blood-drink-fail-reproducing"), popupUser);
            return false;
        }

        if (!HasComp<BloodstreamComponent>(target))
        {
            if (showPopup)
                ShowFailPopup(worm.Owner, Loc.GetString("worm-blood-drink-fail-no-blood"), popupUser);
            return false;
        }

        if (!HasDrainableBlood(target, worm.Comp))
        {
            if (showPopup)
                ShowFailPopup(worm.Owner, Loc.GetString("worm-blood-drink-fail-low-blood"), popupUser);
            return false;
        }

        return IsInDrinkRange(worm.Owner, target, worm.Comp.Range, showPopup);
    }

    private void ShowFailPopup(EntityUid worm, string message, EntityUid? popupUser)
    {
        _popup.PopupPredicted(message, worm, popupUser, PopupType.Small);
    }

    private bool IsInDrinkRange(EntityUid worm, EntityUid target, float range, bool popup = false)
    {
        return _interaction.InRangeUnobstructed(worm, target, range, popup: popup);
    }

    public bool CanDrainBlood(EntityUid worm, EntityUid target, WormBloodDrinkerComponent drinker)
    {
        if (!TryComp<BloodstreamComponent>(target, out var blood))
            return false;

        if (!_solution.ResolveSolution(target, blood.BloodSolutionName, ref blood.BloodSolution, out var solution))
            return false;

        if (solution.Volume < drinker.DrainAmount)
            return false;

        return HasDrainableBlood(target, drinker, solution.Volume - drinker.DrainAmount);
    }

    protected bool HasDrainableBlood(EntityUid target, WormBloodDrinkerComponent drinker, FixedPoint2? volumeAfterNextDrain = null)
    {
        if (!TryComp<BloodstreamComponent>(target, out var blood))
            return false;

        if (!_solution.ResolveSolution(target, blood.BloodSolutionName, ref blood.BloodSolution, out var solution))
            return false;

        var volume = volumeAfterNextDrain ?? solution.Volume;
        var minVolume = blood.BloodReferenceSolution.Volume * FixedPoint2.New(drinker.MinVictimBloodFraction);
        return volume >= minVolume;
    }

    private void CancelDoAfters(EntityUid worm)
    {
        if (!TryComp<DoAfterComponent>(worm, out var comp))
            return;

        foreach (var doAfter in comp.DoAfters.Values.ToArray())
            _doAfter.Cancel(new DoAfterId(worm, doAfter.Index), comp);
    }
}
