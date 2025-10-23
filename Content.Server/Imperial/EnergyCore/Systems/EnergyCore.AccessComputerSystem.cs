using JetBrains.Annotations;
using Robust.Shared.Prototypes;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Timing;
using Content.Server.GameTicking;
using Content.Shared.GameTicking;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Audio;
using Content.Shared.Audio;
using Content.Shared.Imperial.EnergyCore;
using Content.Server.Imperial.EnergyCore.Components;

namespace Content.Server.Imperial.EnergyCore;

public sealed class CoreAccessComputerSystem : EntitySystem
{
    [Dependency] private readonly AppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly ItemSlotsSystem _itemSlots = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CoreAccessComputerComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<CoreAccessComputerComponent, EntInsertedIntoContainerMessage>(OnItemSlotChanged);
        SubscribeLocalEvent<CoreAccessComputerComponent, EntRemovedFromContainerMessage>(OnItemSlotChanged);
    }
    private void OnInit(EntityUid uid, CoreAccessComputerComponent component, ComponentInit args)
    {
        _itemSlots.AddItemSlot(uid, SharedEnergyCoreComponent.DeCodeSlotId, component.DeCodeSlot);
    }
    private void OnItemSlotChanged(EntityUid uid, CoreAccessComputerComponent component, ContainerModifiedMessage args)
    {
        if (!component.Initialized)
            return;

        if (args.Container.ID != component.DeCodeSlot.ID)
            return;

        GetCheckTime(component);
    }
    private void GetCheckTime(CoreAccessComputerComponent component)
    {
        component.Time = _timing.CurTime + component.TimeToCheck;
    }
    private void CompleteProtocolDeactivation(EntityUid uid, CoreAccessComputerComponent component)
    {
        component.SaveProtocolWasDeactivated = true;
        component.TerminalStatus = 2;
    }
    private void UpdateVisual(EntityUid uid, CoreAccessComputerComponent component)
    {
        if (TryComp<AppearanceComponent>(uid, out var appearance))
        {
            _appearance.SetData(uid, CoreStatusScreenVisual.Core_Screen_Visual, component.TerminalStatus, appearance);
        }
    }
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<CoreAccessComputerComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (_timing.CurTime >= comp.Time && comp.DeCodeSlot.HasItem)
            {
                UpdateVisual(uid, comp);
                if (!comp.DeactivationCompleted)
                {
                    comp.DeactivationCompleted = true;
                    CompleteProtocolDeactivation(uid, comp);
                }
            }
        }
    }
}
