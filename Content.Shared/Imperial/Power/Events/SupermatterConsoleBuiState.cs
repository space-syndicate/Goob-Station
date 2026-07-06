using Robust.Shared.Serialization;

namespace Content.Shared.Imperial.Power.Events;

[Serializable, NetSerializable]
public sealed class SupermatterConsoleBuiState(
    bool activated,
    float? temperature = null,
    float? lowerTemperature = null,
    float? upperTemperature = null,
    float? pressure = null,
    float? lowerPressure = null,
    float? upperPressure = null,
    float? integrity = null,
    Color? integrityColor = null,
    string? currentEvent = null)
    : BoundUserInterfaceState
{
    public bool Activated = activated;
    public float? Temperature = temperature;
    public float? LowerTemperature = lowerTemperature;
    public float? UpperTemperature = upperTemperature;
    public float? Pressure = pressure;
    public float? LowerPressure = lowerPressure;
    public float? UpperPressure = upperPressure;
    public float? Integrity = integrity;
    public Color? IntegrityColor = integrityColor;
    public string? CurrentEvent = currentEvent;
}
