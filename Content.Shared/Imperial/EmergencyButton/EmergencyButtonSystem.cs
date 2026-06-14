using Content.Shared.Examine;
using Content.Shared.Imperial.EmergencyButton.Components;
using Content.Shared.Interaction.Events;
using Content.Shared.Popups;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Timing;

namespace Content.Shared.Imperial.EmergencyButton;

public sealed class EmergencyButtonSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = null!;
    [Dependency] private readonly SharedAudioSystem _audio = null!;
    [Dependency] private readonly IGameTiming _timing = null!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<EmergencyButtonComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<EmergencyButtonComponent, UseInHandEvent>(OnUseInHand);
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

        var user = args.User;

        if (entity.Comp.CurrentCharges <= 0)
        {
            _popup.PopupPredicted(Loc.GetString("alert-emergency-button-popup-no-charges"),
                entity,
                user);
            args.Handled = true;
            return;
        }

        if (entity.Comp.NextUnprime == null)
        {
            Prime((entity, entity.Comp), user);
            args.Handled = true;
            return;
        }

        _audio.PlayPredicted(entity.Comp.UseSound, entity.Owner, user);
        ExecuteEmergencyAction((entity, entity.Comp), user);
        args.Handled = true;
    }

    private void ExecuteEmergencyAction(Entity<EmergencyButtonComponent> entity, EntityUid user)
    {
        Unprime(entity);

        entity.Comp.CurrentCharges--;
        Dirty(entity, entity.Comp);

        _popup.PopupPredicted(Loc.GetString("alert-emergency-button-popup-used"),
            entity,
            user);

        var ev = new EmergencyButtonPressedEvent(user);

        RaiseLocalEvent(entity, ref ev);
    }

    private void Prime(Entity<EmergencyButtonComponent> entity, EntityUid user)
    {
        entity.Comp.NextUnprime = _timing.CurTime + entity.Comp.PrimeTime;
        Dirty(entity, entity.Comp);

        _popup.PopupPredicted(Loc.GetString("alert-emergency-button-popup-confirmation"),
            entity,
            user,
            PopupType.LargeCaution);
    }

    private void Unprime(Entity<EmergencyButtonComponent> entity)
    {
        entity.Comp.NextUnprime = null;
        Dirty(entity, entity.Comp);
    }
}

[ByRefEvent]
public readonly record struct EmergencyButtonPressedEvent(EntityUid User);
