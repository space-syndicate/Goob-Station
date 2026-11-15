using Content.Server.Cargo.Systems;
using Content.Server.Hands.Systems;
using Content.Server.Popups;
using Content.Server.Station.Systems;
using Content.Shared._BlackM.Access;
using Content.Shared._BlackM.Passport;
using Content.Shared.Cargo.Components;
using Content.Shared.Containers.ItemSlots;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using System.Diagnostics.CodeAnalysis;

namespace Content.Server._BlackM.Passport;

public sealed class PassportCheckerSystem : EntitySystem
{
    [Dependency] private readonly ItemSlotsSystem     _itemSlots = default!;
    [Dependency] private readonly UserInterfaceSystem _ui         = default!;
    [Dependency] private readonly SharedAudioSystem    _audio     = default!;
    [Dependency] private readonly PassportSystem       _passport  = default!;
    [Dependency] private readonly PopupSystem          _popup     = default!;
    [Dependency] private readonly HandsSystem          _hands     = default!;
    [Dependency] private readonly StationSystem        _station   = default!;
    [Dependency] private readonly CargoSystem          _cargo     = default!;

    private static readonly SoundSpecifier OkSound    = new SoundPathSpecifier("/Audio/_BlackM/Machines/shtamp.ogg");
    private static readonly SoundSpecifier ErrorSound  = new SoundPathSpecifier("/Audio/_BlackM/Machines/shtamp.ogg");

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PassportCheckerComponent, BoundUIOpenedEvent>(OnUiOpened);
        SubscribeLocalEvent<PassportCheckerComponent, EntInsertedIntoContainerMessage>(OnInserted);
        SubscribeLocalEvent<PassportCheckerComponent, EntRemovedFromContainerMessage>(OnRemoved);
        SubscribeLocalEvent<PassportCheckerComponent, ItemSlotInsertAttemptEvent>(OnInsertAttempt);
        SubscribeLocalEvent<PassportCheckerComponent, ItemSlotEjectAttemptEvent>(OnEjectAttempt);

        SubscribeLocalEvent<PassportCheckerComponent, PassportCheckerSelectFieldMessage>(OnSelectField);
        SubscribeLocalEvent<PassportCheckerComponent, PassportCheckerAccuseMessage>(OnAccuse);
        SubscribeLocalEvent<PassportCheckerComponent, PassportCheckerConfirmCleanMessage>(OnConfirmClean);
        SubscribeLocalEvent<PassportCheckerComponent, PassportCheckerEjectMessage>(OnEject);
    }

    private void OnInsertAttempt(EntityUid uid, PassportCheckerComponent comp, ref ItemSlotInsertAttemptEvent args)
    {
        if (args.Slot.ID != comp.SlotId)
            return;

        if (!TryComp<PassportComponent>(args.Item, out var passport))
            return;

        if (passport.Stamp != PassportStampState.None)
        {
            args.Cancelled = true;
            _popup.PopupEntity(Loc.GetString("passport-checker-already-processed"), uid);
        }
    }

    private void OnEjectAttempt(EntityUid uid, PassportCheckerComponent comp, ref ItemSlotEjectAttemptEvent args)
    {
        if (args.Slot.ID != comp.SlotId)
            return;

        if (args.Slot.Item is not { } item || !TryComp<PassportComponent>(item, out var passport))
            return;

        if (passport.Stamp == PassportStampState.None)
        {
            args.Cancelled = true;
            _popup.PopupEntity(Loc.GetString("passport-checker-locked"), uid);
        }
    }

    private void OnUiOpened(EntityUid uid, PassportCheckerComponent comp, BoundUIOpenedEvent args)
        => UpdateUi(uid, comp);

    private void OnInserted(EntityUid uid, PassportCheckerComponent comp, EntInsertedIntoContainerMessage args)
    {
        if (args.Container.ID != comp.SlotId)
            return;

        ResetCheckState(comp);

        if (_itemSlots.TryGetSlot(uid, comp.SlotId, out var slot))
            _itemSlots.SetLock(uid, slot, true);

        UpdateUi(uid, comp);
    }

    private void OnRemoved(EntityUid uid, PassportCheckerComponent comp, EntRemovedFromContainerMessage args)
    {
        if (args.Container.ID != comp.SlotId)
            return;

        ResetCheckState(comp);

        if (_itemSlots.TryGetSlot(uid, comp.SlotId, out var slot))
            _itemSlots.SetLock(uid, slot, false);

        UpdateUi(uid, comp);
    }

    private void OnSelectField(EntityUid uid, PassportCheckerComponent comp, PassportCheckerSelectFieldMessage args)
    {
        comp.SelectedField = args.Field;
        UpdateUi(uid, comp);
    }

    private void OnAccuse(EntityUid uid, PassportCheckerComponent comp, PassportCheckerAccuseMessage args)
    {
        if (!TryGetPassport(uid, comp, out var passportUid, out var passport))
            return;

        if (comp.SelectedField != null
            && passport.HasBureaucraticError
            && passport.ErrorField == comp.SelectedField)
        {
            comp.Result = PassportCheckerResult.CorrectCatch;
            comp.ConfirmedErrorField = comp.SelectedField;
            _audio.PlayPvs(OkSound, uid);
        }
        else
        {
            comp.Result = PassportCheckerResult.WrongAccusation;
            comp.ConfirmedErrorField = null;
            _audio.PlayPvs(ErrorSound, uid);
        }

        _passport.SetStamp(passportUid, PassportStampState.Denied, passport);
        FinishCheck(uid, comp, passportUid, passport, args.Actor, issuePermit: false);

        UpdateUi(uid, comp);
    }

    private void OnConfirmClean(EntityUid uid, PassportCheckerComponent comp, PassportCheckerConfirmCleanMessage args)
    {
        if (!TryGetPassport(uid, comp, out var passportUid, out var passport))
            return;

        if (passport.HasBureaucraticError)
        {
            comp.Result = PassportCheckerResult.Missed;
            comp.ConfirmedErrorField = null;
            _audio.PlayPvs(ErrorSound, uid);

            FineStation(uid, comp.MissedErrorFine, args.Actor);
        }
        else
        {
            comp.Result = PassportCheckerResult.CorrectClean;
            comp.ConfirmedErrorField = null;
            _audio.PlayPvs(OkSound, uid);
        }

        _passport.SetStamp(passportUid, PassportStampState.Approved, passport);
        FinishCheck(uid, comp, passportUid, passport, args.Actor, issuePermit: comp.Result == PassportCheckerResult.CorrectClean);

        UpdateUi(uid, comp);
    }

    private void FinishCheck(EntityUid uid, PassportCheckerComponent comp, EntityUid passportUid, PassportComponent passport, EntityUid actor, bool issuePermit)
    {
        // personal objectives
        var blackmObjectives = EntityManager.System<Content.Server._BlackM.PersonalObjectives.PersonalObjectivesSystem>();
        blackmObjectives.AddUniqueProgress(actor, Content.Shared._BlackM.PersonalObjectives.BlackMPersonalCounters.PassportChecks, passportUid);
        if (comp.Result == PassportCheckerResult.CorrectCatch)
            blackmObjectives.AddUniqueProgress(actor, Content.Shared._BlackM.PersonalObjectives.BlackMPersonalCounters.PassportCatches, passportUid);

        if (_itemSlots.TryGetSlot(uid, comp.SlotId, out var slot))
        {
            _itemSlots.SetLock(uid, slot, false);
            _itemSlots.TryEjectToHands(uid, slot, actor);
        }

        if (!issuePermit)
            return;

        var permit = Spawn(comp.PermitPrototype, Transform(uid).Coordinates);
        if (TryComp<BadgePrintPermitComponent>(permit, out var permitComp))
        {
            permitComp.IssuedFor = $"{passport.Surname} {passport.OwnerName}".Trim();
            Dirty(permit, permitComp);
        }

        _hands.TryPickupAnyHand(actor, permit);
    }

    private void FineStation(EntityUid checkerUid, int amount, EntityUid actor)
    {
        if (amount <= 0)
            return;

        var stationUid = _station.GetOwningStation(checkerUid);
        if (stationUid == null)
            return;

        if (!TryComp<StationBankAccountComponent>(stationUid.Value, out var bank))
            return;

        _cargo.UpdateBankAccount((stationUid.Value, bank), -amount, bank.PrimaryAccount);

        _popup.PopupEntity(Loc.GetString("passport-checker-fine", ("amount", amount)), checkerUid, actor);
    }

    private void OnEject(EntityUid uid, PassportCheckerComponent comp, PassportCheckerEjectMessage args)
    {
        if (!_itemSlots.TryGetSlot(uid, comp.SlotId, out var slot))
            return;

        if (slot.Locked)
        {
            _popup.PopupEntity(Loc.GetString("passport-checker-locked"), uid, args.Actor);
            return;
        }

        _itemSlots.TryEjectToHands(uid, slot, args.Actor);

        ResetCheckState(comp);
        UpdateUi(uid, comp);
    }

    private static void ResetCheckState(PassportCheckerComponent comp)
    {
        comp.SelectedField = null;
        comp.ConfirmedErrorField = null;
        comp.Result = PassportCheckerResult.None;
    }

    private bool TryGetPassport(EntityUid uid, PassportCheckerComponent comp, out EntityUid passportUid, [NotNullWhen(true)] out PassportComponent? passport)
    {
        passportUid = default;

        if (!_itemSlots.TryGetSlot(uid, comp.SlotId, out var slot) || slot.Item is not { } item)
        {
            passport = null;
            return false;
        }

        passportUid = item;
        return TryComp(item, out passport);
    }

    private void UpdateUi(EntityUid uid, PassportCheckerComponent comp)
    {
        var hasPassport = _itemSlots.TryGetSlot(uid, comp.SlotId, out var slot) && slot?.Item != null;

        string? refName = null, refSurname = null, refCity = null, refJob = null, refNumber = null, refDate = null;
        string? docName = null, docSurname = null, docCity = null, docJob = null, docNumber = null, docDate = null;
        NetEntity? ownerEntity = null;

        if (hasPassport && slot!.Item is { } passportEnt && TryComp<PassportComponent>(passportEnt, out var passport))
        {
            refName    = passport.OwnerName;
            refSurname = passport.Surname;
            refCity    = passport.City;
            refJob     = passport.JobTitle;
            refNumber  = passport.PassportNumber;
            refDate    = passport.IssuedDate;

            docName    = passport.OwnerName;
            docSurname = passport.Surname;
            docCity    = passport.City;
            docJob     = passport.JobTitle;
            docNumber  = passport.PassportNumber;
            docDate    = passport.IssuedDate;

            if (passport.HasBureaucraticError && !string.IsNullOrEmpty(passport.ErrorField))
            {
                switch (passport.ErrorField)
                {
                    case "city":           docCity    = passport.ErrorValue; break;
                    case "passportnumber": docNumber  = passport.ErrorValue; break;
                    case "surname":        docSurname = passport.ErrorValue; break;
                    case "name":           docName    = passport.ErrorValue; break;
                    case "job":            docJob     = passport.ErrorValue; break;
                    case "issueddate":     docDate    = passport.ErrorValue; break;
                }
            }

            ownerEntity = passport.OwnerEntity;
        }
        else
        {
            ResetCheckState(comp);
        }

        _ui.SetUiState(uid, PassportCheckerUiKey.Key, new PassportCheckerBoundUserInterfaceState(
            hasPassport,
            refName ?? string.Empty,
            refSurname ?? string.Empty,
            refCity ?? string.Empty,
            refJob ?? string.Empty,
            refNumber ?? string.Empty,
            refDate ?? string.Empty,
            docName ?? string.Empty,
            docSurname ?? string.Empty,
            docCity ?? string.Empty,
            docJob ?? string.Empty,
            docNumber ?? string.Empty,
            docDate ?? string.Empty,
            ownerEntity ?? NetEntity.Invalid,
            comp.SelectedField ?? string.Empty,
            comp.Result,
            comp.ConfirmedErrorField ?? string.Empty));
    }
}
