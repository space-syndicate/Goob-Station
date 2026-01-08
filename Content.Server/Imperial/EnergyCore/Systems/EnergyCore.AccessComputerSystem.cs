using JetBrains.Annotations;
using Robust.Shared.Prototypes;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Timing;
using Content.Shared.Power.Components;
using Content.Server.Power.Components;
using Content.Server.GameTicking;
using Content.Shared.GameTicking;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Audio;
using Content.Shared.Imperial.EnergyCore;
using Content.Shared.Imperial.EnergyCore.Components;
using Content.Server.Imperial.EnergyCore.Components;
using Content.Server.Imperial.EnergyCore.Events;
using Content.Server.Imperial.EnergyCore.Helpers;

namespace Content.Server.Imperial.EnergyCore;

public sealed class CoreAccessComputerSystem : SharedCoreAccessComputerSystem // : EntitySystem
{
    [Dependency] private readonly AppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly ItemSlotsSystem _itemSlots = default!;
    [Dependency] private readonly UserInterfaceSystem _userInterfaceSystem = default!;
    [Dependency] private readonly SharedTransformSystem _transformSystem = default!;
    [Dependency] private readonly CoreSearchSystem _coreHelper = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CoreAccessComputerComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<CoreAccessComputerComponent, EntInsertedIntoContainerMessage>(OnItemSlotChanged);
        SubscribeLocalEvent<CoreAccessComputerComponent, EntRemovedFromContainerMessage>(OnItemSlotChanged);
    }
    private void OnInit(EntityUid uid, CoreAccessComputerComponent component, ComponentInit args)
    {
        _itemSlots.AddItemSlot(uid, SharedCoreAccessComponent.DeCodeSlotId, component.DeCodeSlot);

        component.SearchTime = component.SearchTime + _timing.CurTime;

        UpdateUi(uid, component);
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
    private void UpdateFinalCoef(EntityUid uid, CoreAccessComputerComponent value)
    {
        value.FinalTempChangeCoef = value.Reactivity * value.Halflife; // Халфлайф 3 не будет
    }
    private void GetCurrTemp(EntityUid uid, CoreAccessComputerComponent component)
    {
        var nearestUid = component.ControledCore;
        if (nearestUid == null || !TryComp(nearestUid, out EnergyCoreComponent? nearest))
        {
            return;
        }
        var (coreTemp, coreStatus) = GetCoreInfo(nearest);
        component.CurrCoreTemp = coreTemp;
        component.Status = coreStatus;
    }
    private (float coreTemp, CoreStatus coreStatus) GetCoreInfo(EnergyCoreComponent component)
    {
        var coreTemp = component.CoreTemp;
        var coreStatus = component.Status;

        return (coreTemp, coreStatus);
    }
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<CoreAccessComputerComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            UpdateFinalCoef(uid, comp);
            if (_timing.CurTime >= comp.Time && comp.DeCodeSlot.HasItem)
            {
                UpdateVisual(uid, comp);
                if (!comp.DeactivationCompleted)
                {
                    comp.DeactivationCompleted = true;
                    CompleteProtocolDeactivation(uid, comp);
                }
            }
            GetCurrTemp(uid, comp);

            if (_timing.CurTime < comp.SearchTime) // Ищет только первые 5 секунд
            {
                var nearestUid = _coreHelper.FindNearestEnergyCore(uid, 30f);
                if (nearestUid == null ||
                    !EntityManager.TryGetComponent<EnergyCoreComponent>(nearestUid.Value, out var nearest))
                    return;
                else
                    comp.ControledCore = nearestUid;
            }
        }
    }
    #region public API
    public void ResetTerminal(EntityUid terminalUid, CoreAccessComputerComponent? terminal = null)
    {
        if (!Resolve(terminalUid, ref terminal))
            return;

        GetCheckTime(terminal);
        terminal.DeactivationCompleted = false;
        terminal.SaveProtocolWasDeactivated = false;
        terminal.TerminalStatus = 1;
        UpdateVisual(terminalUid, terminal);
    }
    #endregion
}
