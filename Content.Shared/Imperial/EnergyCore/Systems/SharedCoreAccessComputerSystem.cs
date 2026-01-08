using JetBrains.Annotations;
using Robust.Shared.Prototypes;
using Robust.Shared.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Timing;
using Content.Shared.Power.Components;
using Content.Shared.GameTicking;
using Content.Shared.Audio;
using Content.Shared.Imperial.EnergyCore.Components;

namespace Content.Shared.Imperial.EnergyCore;

public abstract class SharedCoreAccessComputerSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _userInterfaceSystem = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CoreAccessComputerComponent, UiButtonPressedMessage>(OnUiButtonPressed);
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
                component.ByteStatus = 2;
                break;
            case UiButton.RiseTemp:
                component.ByteStatus = 3;
                break;
            case UiButton.CoolTemp:
                component.ByteStatus = 1;
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
        var safeProtocol = !component.SaveProtocolWasDeactivated;
        var tempRising = component.TempRising;
        var coreStatus = component.Status;
        var autoSystem = component.ByteStatus;
        var coreTemp = component.CurrCoreTemp;
        var tempChangeCoef = component.FinalTempChangeCoef;
        var currentPowerSupply = component.CurrentPowerSupply;

        return new CoreTerminalBoundUserInterfaceState(
                coreStatus,
                tempRising,
                safeProtocol,
                autoSystem,
                coreTemp,
                tempChangeCoef,
                currentPowerSupply);
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
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<CoreAccessComputerComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.NextUIUpdate < _timing.CurTime)
                UpdateUi(uid, comp);
        }
    }
    #endregion
}
