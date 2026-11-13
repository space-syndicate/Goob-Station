// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Popups;
using Content.Shared._CorvaxGoob.CardPdaPainter;
using Content.Shared.Access.Components;
using Content.Shared.Clothing.Components;
using Content.Shared.Clothing.EntitySystems;
using Content.Shared.Item;
using Content.Shared.PDA;
using Content.Shared.Roles;
using Robust.Server.Audio;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;

namespace Content.Server._CorvaxGoob.CardPdaPainter;

public sealed class CardPdaPainterSystem : SharedCardPdaPainterSystem
{
    // Some jobs do not put their PDA in startingGear.equipment.id, or their job ID does not match the PDA prototype ID.
    // Keep these aliases here instead of hardcoding separate job handling paths.
    private static readonly Dictionary<string, EntProtoId> JobPdaFallbacks = new()
    {
        { "AtmosphericTechnician", "AtmosPDA" },
        { "Brigmedic", "BrigmedicPDA" },
        { "CargoTechnician", "CargoPDA" },
        { "ChiefEngineer", "CEPDA" },
        { "ChiefMedicalOfficer", "CMOPDA" },
        { "HeadOfPersonnel", "HoPPDA" },
        { "HeadOfSecurity", "HoSPDA" },
        { "MedicalDoctor", "MedicalPDA" },
        { "RadioHost", "RadioHostPDA" },
        { "ResearchDirector", "RnDPDA" },
        { "Roboticist", "RoboticistPDA" },
        { "SalvageSpecialist", "SalvagePDA" },
        { "Scientist", "SciencePDA" },
        { "SecurityOfficer", "SecurityPDA" },
        { "StationEngineer", "EngineerPDA" },
        { "Virologist", "VirologistPDA" },
    };

    [Dependency] private IPrototypeManager _prototype = default!;
    [Dependency] private SharedItemSystem _item = default!;
    [Dependency] private ClothingSystem _clothing = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private UserInterfaceSystem _ui = default!;
    [Dependency] private PopupSystem _popup = default!;
    [Dependency] private AudioSystem _audio = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CardPdaPainterComponent, BoundUIOpenedEvent>(OnUiOpened);
        SubscribeLocalEvent<CardPdaPainterComponent, EntInsertedIntoContainerMessage>(OnSlotChanged);
        SubscribeLocalEvent<CardPdaPainterComponent, EntRemovedFromContainerMessage>(OnSlotChanged);
        SubscribeLocalEvent<CardPdaPainterComponent, CardPdaPainterRepaintMessage>(OnRepaint);
    }

    private void OnUiOpened(Entity<CardPdaPainterComponent> ent, ref BoundUIOpenedEvent args)
    {
        UpdateUi(ent);
    }

    private void OnSlotChanged(Entity<CardPdaPainterComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        if (args.Container.ID == CardPdaPainterComponent.TargetSlotId)
            UpdateUi(ent);
    }

    private void OnSlotChanged(Entity<CardPdaPainterComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        if (args.Container.ID == CardPdaPainterComponent.TargetSlotId)
            UpdateUi(ent);
    }

    private void OnRepaint(Entity<CardPdaPainterComponent> ent, ref CardPdaPainterRepaintMessage args)
    {
        // UI messages are never trusted blindly: the target may have been removed from the slot,
        // or the selected job may no longer resolve to a valid visual template.
        if (ent.Comp.TargetSlot.Item is not { } target)
            return;

        var targetType = GetTargetType(target);
        if (targetType == CardPdaPainterTargetType.None)
            return;

        if (!TryGetVisualPrototype(args.JobId, targetType, out var visualPrototype))
            return;

        if (!ApplyVisualTemplate(target, visualPrototype))
            return;

        _audio.PlayPvs(ent.Comp.PaintSound, ent);
        _popup.PopupEntity(Loc.GetString("card-pda-painter-popup-success"), ent, args.Actor);
        UpdateUi(ent);
    }

    private void UpdateUi(Entity<CardPdaPainterComponent> ent)
    {
        // Rebuild the whole state whenever the window opens or the slot changes.
        // This keeps the client list aligned with whether the inserted target is an ID card or a PDA.
        var target = ent.Comp.TargetSlot.Item;
        var targetType = target is { } targetUid
            ? GetTargetType(targetUid)
            : CardPdaPainterTargetType.None;

        var jobs = targetType == CardPdaPainterTargetType.None
            ? new List<CardPdaPainterJobEntry>()
            : GetJobEntries(targetType);

        var state = new CardPdaPainterBoundUserInterfaceState(
            target != null,
            target != null ? Name(target.Value) : string.Empty,
            targetType,
            jobs);

        _ui.SetUiState(ent.Owner, CardPdaPainterUiKey.Key, state);
    }

    private List<CardPdaPainterJobEntry> GetJobEntries(CardPdaPainterTargetType targetType)
    {
        var entries = new List<CardPdaPainterJobEntry>();

        foreach (var job in _prototype.EnumeratePrototypes<JobPrototype>())
        {
            // Mirror ID console visibility so hidden/admin-only jobs do not appear as regular station styles.
            if (!job.OverrideConsoleVisibility.GetValueOrDefault(job.SetPreference))
                continue;

            if (TryGetVisualPrototype(job.ID, targetType, out var visualPrototype))
                entries.Add(new CardPdaPainterJobEntry(job.ID, visualPrototype));
        }

        entries.Sort((x, y) =>
        {
            var xName = _prototype.Index(x.JobId).LocalizedName;
            var yName = _prototype.Index(y.JobId).LocalizedName;
            return string.Compare(xName, yName, StringComparison.CurrentCulture);
        });

        return entries;
    }

    private bool TryGetVisualPrototype(
        ProtoId<JobPrototype> jobId,
        CardPdaPainterTargetType targetType,
        out EntProtoId visualPrototype)
    {
        visualPrototype = default;

        // A job style starts from its job prototype, then its starting gear, then the item in the "id" slot.
        // Some jobs skip that slot, so the helper methods below try safe fallbacks after this direct path.
        if (!_prototype.TryIndex(jobId, out JobPrototype? job) ||
            job.StartingGear is not { } startingGearId ||
            !_prototype.TryIndex(startingGearId, out StartingGearPrototype? startingGear))
        {
            return false;
        }

        if (targetType == CardPdaPainterTargetType.Pda)
            return TryGetPdaVisualPrototype(job, startingGear, out visualPrototype);

        // For ID cards, prefer the actual card from starting gear. If the job starts with a PDA,
        // use the ID card configured inside that PDA so card and PDA styles stay paired.
        if (startingGear.Equipment.TryGetValue("id", out var idGear) &&
            TryGetIdCardVisualPrototype(idGear, out visualPrototype))
        {
            return true;
        }

        if (TryGetPdaVisualPrototype(job, startingGear, out var pdaPrototype) &&
            TryGetIdCardVisualPrototype(pdaPrototype, out visualPrototype))
        {
            return true;
        }

        var jobIdCard = $"{job.ID}IDCard";
        return TryGetIdCardVisualPrototype(jobIdCard, out visualPrototype);
    }

    private bool TryGetPdaVisualPrototype(
        JobPrototype job,
        StartingGearPrototype startingGear,
        out EntProtoId visualPrototype)
    {
        visualPrototype = default;

        if (startingGear.Equipment.TryGetValue("id", out var idGear) &&
            TryGetPdaVisualPrototype(idGear, out visualPrototype))
        {
            return true;
        }

        var jobPda = $"{job.ID}PDA";
        if (TryGetPdaVisualPrototype(jobPda, out visualPrototype))
            return true;

        // Fall back to common shared PDA prototypes such as SecurityPDA or SciencePDA.
        return JobPdaFallbacks.TryGetValue(job.ID, out var fallback) &&
               TryGetPdaVisualPrototype(fallback, out visualPrototype);
    }

    private bool TryGetPdaVisualPrototype(EntProtoId prototypeId, out EntProtoId visualPrototype)
    {
        visualPrototype = default;

        if (!_prototype.TryIndex<EntityPrototype>(prototypeId, out var prototype) ||
            !prototype.TryGetComponent<PdaComponent>(out _, Factory))
        {
            return false;
        }

        visualPrototype = prototypeId;
        return true;
    }

    private bool TryGetIdCardVisualPrototype(EntProtoId prototypeId, out EntProtoId visualPrototype)
    {
        visualPrototype = default;

        if (!_prototype.TryIndex<EntityPrototype>(prototypeId, out var prototype))
            return false;

        if (prototype.TryGetComponent<PdaComponent>(out var pda, Factory))
        {
            if (string.IsNullOrEmpty(pda.IdCard))
                return false;

            // PDA repainting should not touch the inserted card, but ID-card repainting may use
            // the PDA's configured card prototype as a visual template.
            prototypeId = pda.IdCard;
        }

        if (!_prototype.TryIndex<EntityPrototype>(prototypeId, out var idCardPrototype) ||
            !idCardPrototype.TryGetComponent<IdCardComponent>(out _, Factory))
        {
            return false;
        }

        visualPrototype = prototypeId;
        return true;
    }

    private CardPdaPainterTargetType GetTargetType(EntityUid target)
    {
        if (HasComp<PdaComponent>(target))
            return CardPdaPainterTargetType.Pda;

        if (HasComp<IdCardComponent>(target))
            return CardPdaPainterTargetType.IdCard;

        return CardPdaPainterTargetType.None;
    }

    private bool ApplyVisualTemplate(EntityUid target, EntProtoId visualPrototype)
    {
        if (!_prototype.TryIndex<EntityPrototype>(visualPrototype, out var prototype))
            return false;

        // Store the template ID so the client can rebuild the visual after networking or prediction updates.
        var visualOverride = EnsureComp<CardPdaVisualOverrideComponent>(target);
        visualOverride.VisualPrototype = visualPrototype;
        Dirty(target, visualOverride);

        // Copy only visual-facing components. ID metadata, access, station records, and PDA contents are left intact.
        // In practice this makes the item look like the selected job's card/PDA without making it act like one.
        if (TryComp<ItemComponent>(target, out var item) &&
            prototype.TryGetComponent<ItemComponent>(out var otherItem, Factory))
        {
            _item.CopyVisuals(target, otherItem, item);
        }

        if (TryComp<ClothingComponent>(target, out var clothing) &&
            prototype.TryGetComponent<ClothingComponent>(out var otherClothing, Factory))
        {
            _clothing.CopyVisuals(target, otherClothing, clothing);
        }

        if (TryComp<AppearanceComponent>(target, out var appearance) &&
            prototype.TryGetComponent<AppearanceComponent>(out var otherAppearance, Factory))
        {
            _appearance.AppendData(otherAppearance, target);
            Dirty(target, appearance);
        }

        return true;
    }
}
