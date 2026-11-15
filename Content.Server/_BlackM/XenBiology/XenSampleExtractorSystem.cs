using Content.Shared._BlackM.XenBiology;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Robust.Shared.Containers;
using Robust.Shared.Timing;

namespace Content.Server._BlackM.XenBiology;

public sealed class XenSampleExtractorSystem : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly ItemSlotsSystem _itemSlots = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<XenSampleExtractorComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<XenSampleExtractorComponent, XenSampleExtractDoAfterEvent>(OnExtractDoAfter);
    }

    private void OnAfterInteract(Entity<XenSampleExtractorComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Handled || args.Target == null || !args.CanReach)
            return;

        if (!TryStartExtraction(ent, args.User, args.Target.Value))
            return;

        args.Handled = true;
    }

    private void OnExtractDoAfter(Entity<XenSampleExtractorComponent> ent, ref XenSampleExtractDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Target == null)
            return;

        if (!TryExtractSample(ent, args.User, args.Target.Value))
            return;

        args.Handled = true;
        EntityManager.System<Content.Server._BlackM.PersonalObjectives.PersonalObjectivesSystem>().AddProgress(args.User, Content.Shared._BlackM.PersonalObjectives.BlackMPersonalCounters.XenSamples); // personal objectives
    }

    public bool TryStartExtraction(Entity<XenSampleExtractorComponent> extractor, EntityUid user, EntityUid target)
    {
        if (!CanExtractSample(extractor, user, target))
            return false;

        var source = Comp<XenSampleSourceComponent>(target);
        var args = new DoAfterArgs(EntityManager,
            user,
            source.SampleDelay,
            new XenSampleExtractDoAfterEvent(),
            extractor,
            target: target,
            used: extractor)
        {
            BreakOnDamage = true,
            BreakOnMove = true,
            NeedHand = true,
        };

        if (!_doAfter.TryStartDoAfter(args))
            return false;

        _popup.PopupEntity(Loc.GetString("xen-sample-extractor-start"), target, user);
        return true;
    }

    public bool CanExtractSample(Entity<XenSampleExtractorComponent> extractor,
        EntityUid user,
        EntityUid target,
        bool quiet = false)
    {
        if (!TryComp<XenSampleSourceComponent>(target, out var source))
        {
            if (!quiet)
                _popup.PopupEntity(Loc.GetString("xen-sample-extractor-invalid-target"), user, user);
            return false;
        }

        if (source.NextSampleTime > _timing.CurTime)
        {
            if (!quiet)
                _popup.PopupEntity(Loc.GetString("xen-sample-extractor-source-cooldown"), target, user);
            return false;
        }

        if (!_itemSlots.TryGetSlot(extractor, extractor.Comp.CapsuleSlotId, out var slot) ||
            slot.Item == null)
        {
            if (!quiet)
                _popup.PopupEntity(Loc.GetString("xen-sample-extractor-no-capsule"), extractor, user);
            return false;
        }

        if (!_solutionContainer.TryGetSolution(slot.Item.Value, "beaker", out _, out var solution) ||
            solution.Volume > 0)
        {
            if (!quiet)
                _popup.PopupEntity(Loc.GetString("xen-sample-extractor-capsule-filled"), extractor, user);
            return false;
        }

        return true;
    }

    public bool TryExtractSample(Entity<XenSampleExtractorComponent> extractor, EntityUid user, EntityUid target)
    {
        if (!CanExtractSample(extractor, user, target, quiet: true))
            return false;

        var source = Comp<XenSampleSourceComponent>(target);
        var slot = Comp<ItemSlotsComponent>(extractor).Slots[extractor.Comp.CapsuleSlotId];
        var emptyCapsule = slot.Item!.Value;

        if (slot.ContainerSlot != null)
            _container.Remove(emptyCapsule, slot.ContainerSlot);

        Del(emptyCapsule);

        var filledCapsule = Spawn(source.FilledCapsulePrototype, Transform(extractor).Coordinates);
        if (slot.ContainerSlot != null)
            _container.Insert(filledCapsule, slot.ContainerSlot);

        source.NextSampleTime = _timing.CurTime + source.SampleCooldown;

        _popup.PopupEntity(Loc.GetString("xen-sample-extractor-success"), target, user);
        return true;
    }
}
