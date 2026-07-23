namespace Content.Shared.Imperial.Power.Events;

/// <summary>
/// Вызывается системами ивентом Суперматерии при их окончании
/// </summary>
[ByRefEvent]
public record struct SupermatterEventFinishedEvent(TimeSpan NextEventInterval);
