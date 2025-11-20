using Content.Shared.ActionBlocker;
using Content.Shared.Body.Events;
using Content.Shared.Damage;
using Content.Shared.Emoting;
using Content.Shared.Examine;
using Content.Shared.Hands;
using Content.Shared.Imperial.Medieval.Magic.TurnedToStone.Events;
using Content.Shared.Interaction.Events;
using Content.Shared.Movement.Events;
using Content.Shared.Popups;
using Content.Shared.Rejuvenate;
using Content.Shared.Speech;
using Robust.Shared.Timing;

namespace Content.Shared.Imperial.Medieval.Magic.TurnedToStone;


public abstract partial class SharedTurnedToStoneSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly ActionBlockerSystem _actionBlockerSystem = default!;
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;


    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TurnedToStoneComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<TurnedToStoneComponent, ComponentShutdown>(OnShutdown);

        SubscribeLocalEvent<TurnedToStoneComponent, ExaminedEvent>(OnExamine);
        SubscribeLocalEvent<TurnedToStoneComponent, RejuvenateEvent>(OnRejuvenate);

        #region Shit

        SubscribeLocalEvent<TurnedToStoneComponent, SpeakAttemptEvent>(OnSpeak);
        SubscribeLocalEvent<TurnedToStoneComponent, EmoteAttemptEvent>(OnEmote);
        SubscribeLocalEvent<TurnedToStoneComponent, ShiverAttemptEvent>(OnShiver);
        SubscribeLocalEvent<TurnedToStoneComponent, ChangeDirectionAttemptEvent>(OnChangeDirection);
        SubscribeLocalEvent<TurnedToStoneComponent, UpdateCanMoveEvent>(OnMove);
        SubscribeLocalEvent<TurnedToStoneComponent, AttackAttemptEvent>(OnAttack);
        SubscribeLocalEvent<TurnedToStoneComponent, ConsciousAttemptEvent>(OnConsciousAttempt);
        SubscribeLocalEvent<TurnedToStoneComponent, DropAttemptEvent>(OnDrop);
        SubscribeLocalEvent<TurnedToStoneComponent, GettingInteractedWithAttemptEvent>(OnInteracted);

        #endregion
    }

    protected virtual void OnStartup(EntityUid uid, TurnedToStoneComponent component, ComponentStartup args)
    {
        if (!CanTurnToStone(uid)) return;
        if (!TryComp<DamageableComponent>(uid, out var damageableComponent)) return;

        component.DisposeTime = _timing.CurTime + component.LifeTime;
        component.CachedDamageModifierSetID = damageableComponent.DamageModifierSetId ?? "";

        _actionBlockerSystem.CanAttack(uid);
        _actionBlockerSystem.UpdateCanMove(uid);
        _damageableSystem.SetDamageModifierSetId(uid, component.DamageModifierSetID);

        RaiseLocalEvent(uid, new AfterTurnetToStone());
    }

    protected virtual void OnShutdown(EntityUid uid, TurnedToStoneComponent component, ComponentShutdown args)
    {
        component.Disposed = true;

        _actionBlockerSystem.CanAttack(uid);
        _actionBlockerSystem.UpdateCanMove(uid);
        _damageableSystem.SetDamageModifierSetId(uid, component.CachedDamageModifierSetID);

        RaiseLocalEvent(uid, new AfterBecomeFromStone());
    }

    private void OnExamine(EntityUid uid, TurnedToStoneComponent component, ExaminedEvent args)
    {
        args.PushMarkup(Loc.GetString("turned-to-stone-examine"));
    }

    private void OnRejuvenate(EntityUid uid, TurnedToStoneComponent component, RejuvenateEvent args)
    {
        RemComp<TurnedToStoneComponent>(uid);
    }


    #region Helpers

    protected bool CanTurnToStone(EntityUid uid)
    {
        if (!HasComp<DamageableComponent>(uid)) return false;

        var ev = new BeforeTurnetToStone();
        RaiseLocalEvent(uid, ev);

        return !ev.Cancelled;
    }

    #endregion

    #region Shit
    private void OnSpeak(EntityUid uid, TurnedToStoneComponent component, ref SpeakAttemptEvent args)
    {
        if (component.Disposed) return;

        args.Cancel();
    }

    private void OnEmote(EntityUid uid, TurnedToStoneComponent component, ref EmoteAttemptEvent args)
    {
        if (component.Disposed) return;

        args.Cancel();
    }

    private void OnShiver(EntityUid uid, TurnedToStoneComponent component, ref ShiverAttemptEvent args)
    {
        if (component.Disposed) return;

        args.Cancelled = true;
    }

    private void OnChangeDirection(EntityUid uid, TurnedToStoneComponent component, ref ChangeDirectionAttemptEvent args)
    {
        if (component.Disposed) return;

        args.Cancel();
    }

    private void OnMove(EntityUid uid, TurnedToStoneComponent component, ref UpdateCanMoveEvent args)
    {
        if (component.Disposed) return;

        args.Cancel();
    }

    private void OnAttack(EntityUid uid, TurnedToStoneComponent component, ref AttackAttemptEvent args)
    {
        if (component.Disposed) return;

        args.Cancel();
    }

    private void OnConsciousAttempt(EntityUid uid, TurnedToStoneComponent component, ref ConsciousAttemptEvent args)
    {
        if (component.Disposed) return;

        args.Cancelled = true;
    }

    private void OnDrop(EntityUid uid, TurnedToStoneComponent component, ref DropAttemptEvent args)
    {
        if (component.Disposed) return;

        args.Cancel();
    }

    private void OnInteracted(EntityUid uid, TurnedToStoneComponent component, ref GettingInteractedWithAttemptEvent args)
    {
        if (component.Disposed) return;

        args.Cancelled = true;
    }


    #endregion
}
