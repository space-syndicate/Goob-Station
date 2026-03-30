using Robust.Shared.Serialization;

namespace Content.Shared.Imperial.Atmos.Piping.Binary.Components;

[Serializable, NetSerializable]
public enum HydrogenGasPressurePumpUiKey : byte
{
    Key,
}

[Serializable, NetSerializable]
public sealed class HydrogenGasPressurePumpToggleStatusMessage(bool enabled) : BoundUserInterfaceMessage
{
    public bool Enabled { get; } = enabled;
}

[Serializable, NetSerializable]
public sealed class HydrogenGasPressurePumpChangeOutputPressureMessage(float pressure) : BoundUserInterfaceMessage
{
    public float Pressure { get; } = pressure;
}
