using Content.Shared.Examine;
using Content.Shared.Imperial.EmergencyButton.Components;
using Content.Shared.Imperial.EmergencyButton.Events;
using Content.Shared.Interaction.Events;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Timing;

namespace Content.Shared.Imperial.EmergencyButton;

public sealed class EmergencyButtonSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = null!;
    [Dependency] private readonly IGameTiming _timing = null!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<EmergencyButtonComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<EmergencyButtonComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<EmergencyButtonComponent, GetVerbsEvent<AlternativeVerb>>(OnAlternativeVerb);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<EmergencyButtonComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.NextUnprime is not {} time)
                continue;

            if (now >= time)
                Unprime((uid, comp));
        }
    }

    private void OnExamined(Entity<EmergencyButtonComponent> entity, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;
        args.PushMarkup(entity.Comp.CurrentCharges != 0
            ? Loc.GetString("alert-emergency-button-yes-charges", ("charges", entity.Comp.CurrentCharges))
            : Loc.GetString("alert-emergency-button-no-charges"));
    }

    private void OnUseInHand(Entity<EmergencyButtonComponent> entity, ref UseInHandEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = TryActivate(entity, args.User);
    }

    private void OnAlternativeVerb(Entity<EmergencyButtonComponent> entity, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        var disabled = entity.Comp.CurrentCharges <= 0;

        var largs = args;
        var activateVerb = new AlternativeVerb
        {
            Text = Loc.GetString("alert-emergency-button-verb"),
            Act = () => TryActivate(entity, largs.User, false),
            Disabled = disabled,
        };
        args.Verbs.Add(activateVerb);
    }

    private void ExecuteEmergencyAction(Entity<EmergencyButtonComponent> entity, EntityUid user)
    {
        Unprime(entity);

        entity.Comp.CurrentCharges--;
        DirtyField(entity.Owner, entity.Comp, nameof(EmergencyButtonComponent.CurrentCharges));

        _popup.PopupPredicted(Loc.GetString("alert-emergency-button-popup-used"),
            entity,
            user);

        var ev = new EmergencyButtonPressedEvent(user);

        RaiseLocalEvent(entity, ref ev);
    }

    private void Prime(Entity<EmergencyButtonComponent> entity, EntityUid user)
    {
        entity.Comp.NextUnprime = _timing.CurTime + entity.Comp.PrimeTime;

        _popup.PopupPredicted(Loc.GetString("alert-emergency-button-popup-confirmation"),
            entity,
            user,
            PopupType.LargeCaution);
    }

    private static void Unprime(Entity<EmergencyButtonComponent> entity)
    {
        entity.Comp.NextUnprime = null;
    }

    private bool TryActivate(Entity<EmergencyButtonComponent> entity, EntityUid user, bool checkCharges = true)
    {
        if (checkCharges && entity.Comp.CurrentCharges <= 0)
        {
            _popup.PopupPredicted(Loc.GetString("alert-emergency-button-popup-no-charges"),
                entity,
                user);
            return true;
        }

        if (entity.Comp.NextUnprime == null)
        {
            Prime((entity, entity.Comp), user);
            return true;
        }

        ExecuteEmergencyAction((entity, entity.Comp), user);
        return true;
    }
}
