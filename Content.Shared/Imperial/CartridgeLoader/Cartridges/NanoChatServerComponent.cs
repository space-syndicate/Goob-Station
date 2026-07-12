using Robust.Shared.GameStates;

namespace Content.Shared.Imperial.CartridgeLoader.Cartridges;

[RegisterComponent, NetworkedComponent]
public sealed partial class NanoChatServerComponent : Component
{
    [DataField]
    public Dictionary<NetEntity, NanoChatContact> Users = new();

    [DataField]
    public List<NanoChatMessage> Messages = new();

    [ViewVariables]
    public Dictionary<int, NanoChatChat> Chats = new();

    [ViewVariables]
    public int NextChatId = 1;

    [ViewVariables]
    public readonly HashSet<EntityUid> ConnectedClients = new();
}
