using Content.Shared.Whitelist;

namespace Content.Server.Imperial.Power.Components;

[RegisterComponent]
public sealed partial class SupermatterEventSchedulerComponent : Component
{
    /// <summary>
    /// Список компонентов событий Суперматерии
    /// </summary>
    [DataField]
    public EntityWhitelist? Events;

    /// <summary>
    /// Максимальное число событий которое может происходить одновременно
    /// </summary>
    [DataField]
    public int MaxConcurrentEvents = 3;

    /// <summary>
    /// Текущее число событий происходящих одновременно
    /// </summary>
    [DataField]
    public int ConcurrentEvents;

    /// <summary>
    /// Отключены ли события Суперматерии
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public bool Disabled;

    /// <summary>
    /// Время когда появится новое событие Суперматерии
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan? NextEventTime;

    /// <summary>
    /// Время первого события Суперматерии с её активации (не гибом)
    /// </summary>
    [DataField]
    public TimeSpan? FirstEventInterval;
}
