using Robust.Shared.GameStates;

namespace Content.Shared.Imperial.CartridgeLoader.Cartridges;

[RegisterComponent, NetworkedComponent]
public sealed partial class NanoChatServerComponent : Component
{
    [DataField]
    public Dictionary<NetEntity, NanoChatContact> Users = new();

    [ViewVariables]
    public List<NanoChatChat> Chats = new();

    [ViewVariables]
    public int NextChatId = 1;

    [DataField]
    public Dictionary<int, Dictionary<NetEntity, TimeSpan>> TypingTimeouts = new();

    [DataField]
    public TimeSpan TypingTimeout = TimeSpan.FromSeconds(5);
}
