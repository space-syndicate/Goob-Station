namespace Content.Server.Imperial.Power.Components.EventComponents;

public interface ISupermatterEventComponent
{
    /// <summary>
    /// Имя события, отображаемое в консоли мониторинга суперматерии
    /// </summary>
    LocId? EventName { get; }
}
