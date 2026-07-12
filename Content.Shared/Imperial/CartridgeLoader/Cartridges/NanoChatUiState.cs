using Robust.Shared.Serialization;

namespace Content.Shared.Imperial.CartridgeLoader.Cartridges;

[Serializable, NetSerializable]
public readonly record struct NanoChatContact(NetEntity Id, string Name, string? JobTitle, string? JobIconId);

[Serializable, NetSerializable]
public sealed class NanoChatMessage(int chatId, NetEntity senderId, string senderName, string content, TimeSpan sendTime)
{
    public int ChatId = chatId;
    public NetEntity SenderId = senderId;
    public string SenderName = senderName;
    public string Content = content;
    public TimeSpan SendTime = sendTime;
}

[Serializable, NetSerializable]
public sealed class NanoChatChat(int id, string name, List<NetEntity> members, bool automated = true)
{
    public int Id = id;
    public string Name = name;
    public List<NetEntity> Members = members;
    public bool Automated = automated;
}

[Serializable, NetSerializable]
public sealed class NanoChatBoundUserInterfaceState(
    bool notificationOn,
    NetEntity? currentUserId,
    NanoChatChat? currentChat,
    List<NanoChatChat> chats,
    List<NanoChatContact> contacts,
    List<NanoChatMessage> messages,
    bool isServerOnline,
    bool isContactReachable,
    bool canSendLocation,
    Dictionary<int, int> unreadMessages)
    : BoundUserInterfaceState
{
    public bool NotificationOn = notificationOn;
    public NetEntity? CurrentUserId = currentUserId;
    public NanoChatChat? CurrentChat = currentChat;
    public List<NanoChatChat> Chats = chats;
    public List<NanoChatContact> Contacts = contacts;
    public List<NanoChatMessage> Messages = messages;
    public bool IsServerOnline = isServerOnline;
    public bool IsContactReachable = isContactReachable;
    public bool CanSendLocation = canSendLocation;
    public Dictionary<int, int> UnreadMessages = unreadMessages;
}
