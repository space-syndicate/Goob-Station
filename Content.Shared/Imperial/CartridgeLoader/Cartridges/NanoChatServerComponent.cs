using Content.Shared.Whitelist;
using Robust.Shared.GameStates;

namespace Content.Shared.Imperial.CartridgeLoader.Cartridges;

/// <summary>
/// Компонент сервера НаноЧата, поддерживающего работу мессенджера.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class NanoChatServerComponent : Component
{
    /// <summary>
    /// Если установлен, сервер будет принимать подключения только от картриджей подходящих под белый список.
    /// </summary>
    [DataField]
    public EntityWhitelist? CartridgeWhitelist;

    /// <summary>
    /// Список пользователей сервера
    /// </summary>
    [DataField]
    public List<NanoChatContact> Users = [];

    /// <summary>
    /// Список чатов сервера
    /// </summary>
    [ViewVariables]
    public readonly List<NanoChatChat> Chats = [];

    /// <summary>
    /// Следующий ID для создаваемого чата
    /// </summary>
    [ViewVariables]
    public int NextChatId = 1;

    /// <summary>
    /// Все записи когда должно перестать показываться что пользователь печатает
    /// </summary>
    [DataField]
    public Dictionary<int, Dictionary<NetEntity, TimeSpan>> TypingTimeouts = new();

    /// <summary>
    /// Сколько показывается что пользователь печатает
    /// </summary>
    [DataField]
    public TimeSpan TypingTimeout = TimeSpan.FromSeconds(5);
}
