namespace Content.Shared.Imperial.EmergencyButton.Events;

[ByRefEvent]
public readonly record struct EmergencyButtonPressedEvent(EntityUid User);
