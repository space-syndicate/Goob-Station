using Robust.Shared.Serialization;

namespace Content.Shared.Imperial.XxRaay.Nda079.Events;

/// <summary>
/// Событие от клиента на сервер с выбранным действием для шлюза
/// </summary>
[Serializable, NetSerializable]
public sealed class NDA079AirlockActionEvent : EntityEventArgs
{
    /// <summary>
    /// Целевой шлюз
    /// </summary>
    public NetEntity Target { get; init; }

    /// <summary>
    /// Тип действия
    /// </summary>
    public NDA079AirlockActionType ActionType { get; init; }
}

/// <summary>
/// Типы действий для шлюза
/// </summary>
[Serializable, NetSerializable]
public enum NDA079AirlockActionType : byte
{
    /// <summary>
    /// Открыть или закрыть шлюз
    /// </summary>
    Toggle,

    /// <summary>
    /// Заболтовать шлюз на время
    /// </summary>
    Bolt
}
