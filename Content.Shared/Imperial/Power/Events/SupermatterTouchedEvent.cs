namespace Content.Shared.Imperial.Power.Events;

/// <summary>
/// Вызывается при касании Суперматерии мобом
/// </summary>
[ByRefEvent]
public record struct SupermatterTouchedEvent(bool Cancelled = false);
