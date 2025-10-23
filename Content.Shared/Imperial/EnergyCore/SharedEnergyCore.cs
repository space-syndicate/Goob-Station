using Color = Robust.Shared.Maths.Color;
using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.Imperial.EnergyCore;

public abstract partial class SharedEnergyCoreComponent : Component
{
    public const string DeCodeSlotId = "Code";
}

public enum CoreStatus : byte
{
    OFFLINE = 1,
    IDLE = 2,
    STABLE = 3,
    OPTIMAL = 4,
    MODERATE = 5,
    HIGH = 6,
    CRITICAL_HIGH = 7,
    CATASTROPHIC = 8,
    SAFE_PROTOCOL = 9
}

public enum CoreStatusColorVisual : byte
{
    OFFLINE = 1,
    IDLE = 2,
    STABLE = 3,
    OPTIMAL = 4,
    MODERATE = 5,
    HIGH = 6,
    CRITICAL_HIGH = 7,
    CATASTROPHIC = 8,
    SAFE_PROTOCOL = 9
}

public enum CoreRisingChange : byte
{
    HEATING,
    COOLING
}
public enum CoreTempChangeLevel : byte
{
    STANDART,
    HIGH
}
//TODO: можно добавить доп. уровень: VERY_HIGH

[Serializable, NetSerializable]
public enum CoreStatusVisual : byte
{
    Core_Visual
}

[Serializable, NetSerializable]
public enum CoreStatusScreenVisual : byte
{
    Core_Screen_Visual
}
