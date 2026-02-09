namespace Content.Server.Imperial.XxRaay.Components;

/// <summary>
/// Компонент для сообщения при добавлении сознания сущности
/// </summary>
[RegisterComponent]
public sealed partial class MessageOnMindComponent : Component
{
    /// <summary>
    /// Текст сообщения
    /// </summary>
    [DataField("message")]
    public string Message = string.Empty;

    [ViewVariables]
    public bool SentMessage = false;
}
