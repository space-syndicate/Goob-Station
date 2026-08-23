using Robust.Shared.Serialization;

namespace Content.Shared.Imperial.Power.Events;

[Serializable, NetSerializable]
public sealed class SupermatterConsoleBuiState(
    bool activated,
    float? temperature = null,
    (float?, float?) temperatureThresholds = default,
    float? pressure = null,
    (float?, float?) pressureThresholds = default,
    float? integrity = null,
    Color? integrityColor = null,
    string? currentEvent = null)
    : BoundUserInterfaceState
{
    public bool Activated = activated;
    public float? Temperature = temperature;
    public (float?, float?) TemperatureThresholds = temperatureThresholds;
    public float? Pressure = pressure;
    public (float?, float?) PressureThresholds = pressureThresholds;
    public float? Integrity = integrity;
    public Color? IntegrityColor = integrityColor;
    public string? CurrentEvent = currentEvent;
}
