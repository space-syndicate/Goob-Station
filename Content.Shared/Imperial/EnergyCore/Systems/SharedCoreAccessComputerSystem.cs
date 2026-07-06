using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Timing;
using Content.Shared.Silicons.StationAi;
using Content.Shared.Emag.Systems;
using Content.Shared.Emag.Components;
using Content.Shared.Imperial.EnergyCore.Components;

namespace Content.Shared.Imperial.EnergyCore;

public abstract class SharedCoreAccessComputerSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _userInterfaceSystem = default!;
    [Dependency] private readonly SharedStationAiSystem _ai = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CoreAccessComputerComponent, UiButtonPressedMessage>(OnUiButtonPressed);
        SubscribeLocalEvent<CoreAccessComputerComponent, GotEmaggedEvent>(OnEmagged);
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
                component.TempRiseTerminal = CoreTempChangeLevel.AUTO;
                break;
            case UiButton.RiseTemp:
                component.TempRiseTerminal = CoreTempChangeLevel.HEATING;
                break;
            case UiButton.CoolTemp:
                component.TempRiseTerminal = CoreTempChangeLevel.COOLING;
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
    private bool PlayerCanUseController(EntityUid uid, EntityUid playerEntity, CoreAccessComputerComponent? component = null)
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
        var autoSystem = component.TempRiseTerminal;
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
        if (component.Reactivity >= component.ReactivityMaxCap)
        {
            _audio.PlayPvs(component.CantSound, uid, AudioParams.Default.WithVolume(-2f));
            return;
        }
        component.Reactivity = component.Reactivity + 10;
    }
    private void MakeHalflifeUp(EntityUid uid, CoreAccessComputerComponent component)
    {
        if (component.Halflife >= component.HalflifeMaxCap)
        {
            _audio.PlayPvs(component.CantSound, uid, AudioParams.Default.WithVolume(-2f));
            return;
        }
        component.Halflife = component.Halflife + 1;
    }
    private void MakeReactivityDown(EntityUid uid, CoreAccessComputerComponent component)
    {
        if (component.Reactivity <= component.ReactivityMinCap)
        {
            _audio.PlayPvs(component.CantSound, uid, AudioParams.Default.WithVolume(-2f));
            return;
        }
        component.Reactivity = component.Reactivity - 10;
    }
    private void MakeHalflifeDown(EntityUid uid, CoreAccessComputerComponent component)
    {
        if (component.Halflife <= component.HalflifeMinCap)
        {
            _audio.PlayPvs(component.CantSound, uid, AudioParams.Default.WithVolume(-2f));
            return;
        }
        component.Halflife = component.Halflife - 1;
    }
    private void OnEmagged(EntityUid uid, CoreAccessComputerComponent component, ref GotEmaggedEvent args)
    {
        if (HasComp<EmaggedComponent>(uid)) return;

        component.ReactivityMaxCap = 150f;
        component.HalflifeMaxCap = 20;

        if (TryComp<StationAiWhitelistComponent>(uid, out var aiAccess))
            _ai.SetWhitelistEnabled((uid, aiAccess), false, false);

        _userInterfaceSystem.CloseUi(uid, CoreTerminalUiKey.Key);

        args.Repeatable = false;
        args.Handled = true;
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
