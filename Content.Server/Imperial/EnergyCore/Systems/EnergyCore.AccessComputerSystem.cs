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
using Content.Server.Imperial.EnergyCore.Components;

namespace Content.Server.Imperial.EnergyCore;

public sealed class CoreAccessComputerSystem : EntitySystem
{
    [Dependency] private readonly AppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly ItemSlotsSystem _itemSlots = default!;
    [Dependency] private readonly UserInterfaceSystem _userInterfaceSystem = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CoreAccessComputerComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<CoreAccessComputerComponent, UiButtonPressedMessage>(OnUiButtonPressed);
        SubscribeLocalEvent<CoreAccessComputerComponent, EntInsertedIntoContainerMessage>(OnItemSlotChanged);
        SubscribeLocalEvent<CoreAccessComputerComponent, EntRemovedFromContainerMessage>(OnItemSlotChanged);
    }
    private void OnInit(EntityUid uid, CoreAccessComputerComponent component, ComponentInit args)
    {
        _itemSlots.AddItemSlot(uid, SharedEnergyCoreComponent.DeCodeSlotId, component.DeCodeSlot);
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
    #region UI
    private void OnUiButtonPressed(EntityUid uid, CoreAccessComputerComponent component, UiButtonPressedMessage msg)
    {
        var user = msg.Actor;
        if (!Exists(user))
            return;

        if (!PlayerCanUseController(uid, user, component))
            return;

        _audio.PlayPvs(component.ClickSound, uid, AudioParams.Default.WithVolume(-2f));
        switch (msg.Button)
        {
            case UiButton.Auto:
                TurnAutoSystem(component);
                break;
            case UiButton.RiseTemp:
                MakeCoreTempRise(component);
                break;
            case UiButton.CoolTemp:
                MakeCoreTempCool(component);
                break;
            case UiButton.UpReactivity:
                MakeReactivityUp(uid, component);
                break;
            case UiButton.DownReactivity:
                MakeReactivityDown(uid, component);
                break;
            case UiButton.UpHalflife:
                MakeHalflifeUp(uid, component);
                break;
            case UiButton.DownHalflife:
                MakeHalflifeDown(uid, component);
                break;
        }
        UpdateUi(uid, component);
    }
    private bool PlayerCanUseController(EntityUid uid, EntityUid playerEntity, CoreAccessComputerComponent? component = null)//, bool needsPower = true)
    {
        if (!Resolve(uid, ref component))
            return false;

        if (!Exists(playerEntity))
            return false;

        //if (needsPower && TryComp<ApcPowerReceiverComponent>(uid, out var powerSource) && !powerSource.Powered)
        //    return false;

        return true;
    }
    public void UpdateUi(EntityUid uid, CoreAccessComputerComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        if (!_userInterfaceSystem.HasUi(uid, CoreTerminalUiKey.Key))
            return;

        var state = GetUiState(uid, component);
        _userInterfaceSystem.SetUiState(uid, CoreTerminalUiKey.Key, state);

        component.NextUIUpdate = _timing.CurTime + component.UpdateUIPeriod;
    }
    private CoreTerminalBoundUserInterfaceState GetUiState(EntityUid uid, CoreAccessComputerComponent component)
    {
        var powered = !TryComp<ApcPowerReceiverComponent>(uid, out var powerSource) || powerSource.Powered;

        var safeProtocol = !component.SaveProtocolWasDeactivated;
        var tempRising = component.TempRising;
        var coreStatus = component.CoreStatus;
        var autoSystem = component.ByteStatus;
        float coreTemp = component.CoreTemp;
        float tempChangeCoef = component.FinalTempChangeCoef;
        float currentPowerSupply = component.CurrentPowerSupply;

        if (!powered)
            return new CoreTerminalBoundUserInterfaceState(
                coreStatus,
                tempRising,
                safeProtocol,
                autoSystem,
                coreTemp,
                tempChangeCoef,
                currentPowerSupply);
        else
            return new CoreTerminalBoundUserInterfaceState(
                coreStatus,
                tempRising,
                safeProtocol,
                autoSystem,
                coreTemp,
                tempChangeCoef,
                currentPowerSupply);
    }
    public void TurnAutoSystem(CoreAccessComputerComponent component)
    {
        component.ByteStatus = 2;
    }
    public void MakeCoreTempRise(CoreAccessComputerComponent component)
    {
        component.ByteStatus = 3;
    }
    public void MakeCoreTempCool(CoreAccessComputerComponent component)
    {
        component.ByteStatus = 1;
    }
    private void MakeReactivityUp(EntityUid uid, CoreAccessComputerComponent component)
    {
        if (component.Reactivity >= 100)
        {
            _audio.PlayPvs(component.CantSound, uid, AudioParams.Default.WithVolume(-2f));
            return;
        }
        component.Reactivity = component.Reactivity + 10;
    }
    private void MakeHalflifeUp(EntityUid uid, CoreAccessComputerComponent component)
    {
        if (component.Halflife >= 10)
        {
            _audio.PlayPvs(component.CantSound, uid, AudioParams.Default.WithVolume(-2f));
            return;
        }
        component.Halflife = component.Halflife + 1;
    }
    private void MakeReactivityDown(EntityUid uid, CoreAccessComputerComponent component)
    {
        if (component.Reactivity <= 30)
        {
            _audio.PlayPvs(component.CantSound, uid, AudioParams.Default.WithVolume(-2f));
            return;
        }
        component.Reactivity = component.Reactivity - 10;
    }
    private void MakeHalflifeDown(EntityUid uid, CoreAccessComputerComponent component)
    {
        if (component.Halflife <= 5)
        {
            _audio.PlayPvs(component.CantSound, uid, AudioParams.Default.WithVolume(-2f));
            return;
        }
        component.Halflife = component.Halflife - 1;
    }
    #endregion
    #region EndUI
    #endregion
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<CoreAccessComputerComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            UpdateUi(uid, comp);
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
        }
    }
}
