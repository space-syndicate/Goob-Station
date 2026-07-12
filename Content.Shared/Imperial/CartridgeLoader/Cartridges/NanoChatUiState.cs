using Robust.Shared.Serialization;

namespace Content.Shared.Imperial.CartridgeLoader.Cartridges;

[Serializable, NetSerializable]
public readonly record struct NanoChatContact(NetEntity Id, string Name, string? JobTitle, string? JobIconId);

[Serializable, NetSerializable]
public sealed class NanoChatMessage(int chatId, NetEntity senderId, string senderName, string content)
{
    public int ChatId = chatId;
    public NetEntity SenderId = senderId;
    public string SenderName = senderName;
    public string Content = content;
}

[Serializable, NetSerializable]
public sealed class NanoChatChat(int id, string name, List<NetEntity> members)
{
    public int Id = id;
    public string Name = name;
    public List<NetEntity> Members = members;
}

[Serializable, NetSerializable]
public sealed class NanoChatBoundUserInterfaceState(
    bool notificationOn,
    NetEntity? currentUserId,
    NanoChatChat? currentChat,
    string? pdaCardName,
    List<NanoChatChat> chats,
    List<NanoChatContact> contacts,
    List<NanoChatMessage> messages,
    bool isServerOnline,
    bool isContactReachable,
    Dictionary<int, int> unreadMessages)
    : BoundUserInterfaceState
{
    public bool NotificationOn = notificationOn;
    public NetEntity? CurrentUserId = currentUserId;
    public NanoChatChat? CurrentChat = currentChat;
    public string? PdaCardName = pdaCardName;
    public List<NanoChatChat> Chats = chats;
    public List<NanoChatContact> Contacts = contacts;
    public List<NanoChatMessage> Messages = messages;
    public bool IsServerOnline = isServerOnline;
    public bool IsContactReachable = isContactReachable;
    public Dictionary<int, int> UnreadMessages = unreadMessages;
}
