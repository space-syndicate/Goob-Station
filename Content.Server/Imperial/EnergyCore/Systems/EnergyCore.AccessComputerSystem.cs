using Robust.Shared.Prototypes;
using Robust.Shared.GameObjects;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.Timing;
using Content.Shared.Power.Components;
using Content.Server.Power.Components;
using Content.Server.GameTicking;
using Content.Shared.GameTicking;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Imperial.EnergyCore;
using Content.Shared.Imperial.EnergyCore.Components;
using Content.Server.Imperial.EnergyCore.Components;
using Content.Server.Imperial.EnergyCore.Events;
using Content.Server.Imperial.EnergyCore.Helpers;

namespace Content.Server.Imperial.EnergyCore;

public sealed class CoreAccessComputerSystem : SharedCoreAccessComputerSystem
{
    [Dependency] private readonly AppearanceSystem _appearance = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly ItemSlotsSystem _itemSlots = default!;
    [Dependency] private readonly CoreSearchSystem _coreHelper = default!;
    [Dependency] private readonly EnergyCoreSystem _core = default!;
    public HashSet<EntityUid> CoreTerminalHash = new();
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CoreAccessComputerComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<CoreAccessComputerComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<CoreAccessComputerComponent, EntInsertedIntoContainerMessage>(OnItemSlotChanged);
        SubscribeLocalEvent<CoreAccessComputerComponent, EntRemovedFromContainerMessage>(OnItemSlotChanged);
        SubscribeLocalEvent<CoreInitEvent>(OnNewCoreInit);
    }
    private void OnInit(EntityUid uid, CoreAccessComputerComponent terminal, ComponentInit args)
    {
        CoreTerminalHash.Add(uid);
        RaiseLocalEvent(new CoreTerminalInitEvent());
        _itemSlots.AddItemSlot(uid, SharedCoreAccessComponent.DeCodeSlotId, terminal.DeCodeSlot);
        terminal.SearchTime = terminal.SearchTime + _timing.CurTime;

        RejoinCore(uid, terminal);
        UpdateUi(uid, terminal);
    }
    private void OnShutdown(EntityUid uid, CoreAccessComputerComponent terminal, ComponentShutdown args)
        => CoreTerminalHash.Remove(uid);
    private void RejoinCore(EntityUid uid, CoreAccessComputerComponent terminal)
        => terminal.ControledCore = _coreHelper.FindNearestEnergyCore(uid, _core.CoreHash, 30f);
    private void OnNewCoreInit(CoreInitEvent ev)
    {
        var query = EntityQueryEnumerator<CoreAccessComputerComponent>();
        while (query.MoveNext(out var uid, out var terminal))
        {
            RejoinCore(uid, terminal);
        }
    }
    private void OnItemSlotChanged(EntityUid uid, CoreAccessComputerComponent terminal, ContainerModifiedMessage args)
    {
        if (!terminal.Initialized)
            return;

        if (args.Container.ID != terminal.DeCodeSlot.ID)
            return;

        GetCheckTime(terminal);
    }
    private void GetCheckTime(CoreAccessComputerComponent terminal)
    {
        terminal.Time = _timing.CurTime + terminal.TimeToCheck;
    }
    private void CompleteProtocolDeactivation(EntityUid uid, CoreAccessComputerComponent terminal)
    {
        terminal.SaveProtocolWasDeactivated = true;
        terminal.TerminalStatus = 2;
    }
    private void UpdateVisual(EntityUid uid, CoreAccessComputerComponent terminal)
    {
        if (TryComp<AppearanceComponent>(uid, out var appearance))
        {
            _appearance.SetData(uid, CoreStatusScreenVisual.Core_Screen_Visual, terminal.TerminalStatus, appearance);
        }
    }
    private void UpdateFinalCoef(EntityUid uid, CoreAccessComputerComponent value)
    {
        value.FinalTempChangeCoef = value.Reactivity * value.Halflife; // Халфлайф 3 не будет
    }
    private void GetCurrTemp(EntityUid uid, CoreAccessComputerComponent terminal)
    {
        var nearestUid = terminal.ControledCore;
        if (nearestUid == null || !TryComp(nearestUid, out EnergyCoreComponent? nearest))
        {
            return;
        }
        var (coreTemp, coreStatus) = GetCoreInfo(nearest);
        terminal.CurrCoreTemp = coreTemp;
        terminal.Status = coreStatus;
    }
    private (float coreTemp, CoreStatus coreStatus) GetCoreInfo(EnergyCoreComponent terminal)
    {
        var coreTemp = terminal.CoreTemp;
        var coreStatus = terminal.Status;

        return (coreTemp, coreStatus);
    }
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<CoreAccessComputerComponent>();
        while (query.MoveNext(out var uid, out var terminal))
        {
            UpdateFinalCoef(uid, terminal);
            if (_timing.CurTime >= terminal.Time && terminal.DeCodeSlot.HasItem)
            {
                UpdateVisual(uid, terminal);
                if (!terminal.DeactivationCompleted)
                {
                    terminal.DeactivationCompleted = true;
                    CompleteProtocolDeactivation(uid, terminal);
                }
            }
            GetCurrTemp(uid, terminal);
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
