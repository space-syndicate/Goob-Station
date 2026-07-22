namespace Content.Shared.Imperial.Power.Events;

/// <summary>
/// Вызывается и отправляет сообщение от имени Консоли Мониторинга Суперматерии в чат/рацию
/// </summary>
[ByRefEvent]
public readonly record struct SupermatterSendRadioEvent(string Message);
