using Robust.Shared.Serialization;

namespace Content.Shared.Imperial.EnergyCore;

[Serializable, NetSerializable]
public enum CoreTerminalUiKey
{
    Key
}

public enum UiButton
{
    Auto,
    RiseTemp,
    CoolTemp,
    UpReactivity,
    DownReactivity,
    UpHalflife,
    DownHalflife
}

public enum AutoSystemStatus : byte
{
    ACTIVE,
    NONACTIVE
}

public enum CoreStatus : byte
{
    OFFLINE = 1,
    IDLE = 2,
    STABLE = 3,
    OPTIMAL = 4,
    MODERATE = 5,
    HIGH = 6,
    CRITICALHIGH = 7,
    CATASTROPHIC = 8,
    SAFEPROTOCOL = 9
}

public enum CoreStatusColorVisual : byte
{
    OFFLINE = 1,
    IDLE = 2,
    STABLE = 3,
    OPTIMAL = 4,
    MODERATE = 5,
    HIGH = 6,
    CRITICALHIGH = 7,
    CATASTROPHIC = 8,
    SAFEPROTOCOL = 9
}
public enum CoreTempChangeLevel : byte
{
    HEATING = 1,
    AUTO = 2,
    COOLING = 3
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
