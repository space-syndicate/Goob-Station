using Robust.Shared.Serialization;

namespace Content.Shared.Imperial.EnergyCore;
[Serializable, NetSerializable]
public sealed class CoreTerminalBoundUserInterfaceState : BoundUserInterfaceState
{
    public readonly CoreStatus Status;
    public readonly bool TempRising;
    public readonly bool SafeProtocol;
    public readonly CoreTempChangeLevel AutoSystem;
    public readonly float CoreTemp;
    public readonly float TempChangeCoef;
    public readonly float CurrentPowerSupply;

    public CoreTerminalBoundUserInterfaceState(CoreStatus status, bool tempRising, bool safeProtocol, CoreTempChangeLevel autoSystem, float coreTemp, float tempChangeCoef, float currentPowerSupply)
    {
        Status = status;
        TempRising = tempRising;
        SafeProtocol = safeProtocol;
        AutoSystem = autoSystem;
        CoreTemp = coreTemp;
        TempChangeCoef = tempChangeCoef;
        CurrentPowerSupply = currentPowerSupply;
    }
}
