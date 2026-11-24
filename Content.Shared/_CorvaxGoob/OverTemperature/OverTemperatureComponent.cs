using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._CorvaxGoob.OverTemperature;

[Serializable, NetSerializable]
public enum OverTemperatureStatus
{
    Okay,
    Over,
    Crit
}

[Serializable, NetSerializable]
public enum OverTemperatureStatusInfo
{
    Status
}
