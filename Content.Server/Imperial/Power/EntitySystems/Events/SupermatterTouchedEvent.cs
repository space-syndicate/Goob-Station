namespace Content.Server.Imperial.Power.EntitySystems.Events;

[ByRefEvent]
public record struct SupermatterTouchedEvent(bool Cancelled = false);
