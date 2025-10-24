namespace Content.Shared.Imperial.Mobs.Phantomor
{
    /// <summary>
    /// срабатывает перед изменением возможности движения
    /// </summary>
    [ByRefEvent]
    public readonly record struct BeforeMovementBlockedEvent(EntityUid Entity, bool Blocking, bool Cancel);

    /// <summary>
    /// срабатывает в момент изменением возможности движения
    /// </summary>
    [ByRefEvent]
    public readonly record struct MovementBlockChangedEvent(EntityUid Entity, bool IsBlocked);

    /// <summary>
    /// срабатывает после изменением возможности движения
    /// </summary>
    [ByRefEvent]
    public readonly record struct AfterMovementBlockedEvent(EntityUid Entity, bool IsBlocked);
}
