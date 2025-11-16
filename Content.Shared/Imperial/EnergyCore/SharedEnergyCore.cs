using Robust.Shared.Serialization;

namespace Content.Shared.Imperial.EnergyCore;

public abstract partial class SharedEnergyCoreComponent : Component
{
    public const string DeCodeSlotId = "Code";
}

[Serializable, NetSerializable]
public sealed class CoreTerminalBoundUserInterfaceState : BoundUserInterfaceState
{
    public readonly CoreStatus Status;
    public readonly bool TempRising;
    public readonly bool SafeProtocol;
    public readonly byte AutoSystem;
    public readonly float CoreTemp;
    public readonly float TempChangeCoef;
    public readonly float CurrentPowerSupply;

    public CoreTerminalBoundUserInterfaceState(CoreStatus status, bool tempRising, bool safeProtocol, byte autoSystem, float coreTemp, float tempChangeCoef, float currentPowerSupply)
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

[Serializable, NetSerializable]
public sealed class UiButtonPressedMessage : BoundUserInterfaceMessage
{
    public readonly UiButton Button;

    public UiButtonPressedMessage(UiButton button)
    {
        Button = button;
    }
}
public sealed class CoreLineEditAdjustMessage : BoundUserInterfaceMessage
{
    /// <summary>
    /// Реактивность ядра
    /// </summary>
    public float ReactivityMsg;

    /// <summary>
    /// Распад ядра
    /// </summary>
    public float HalflifeMsg;
}
