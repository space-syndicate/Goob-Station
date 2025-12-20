using Robust.Shared.Serialization;

namespace Content.Shared.Imperial.ImperialVehicle.Enums;

[Serializable, NetSerializable]
public enum VehicleVisuals : byte
{
    DrawDepth,
    AutoAnimate,
    HideRider
}
