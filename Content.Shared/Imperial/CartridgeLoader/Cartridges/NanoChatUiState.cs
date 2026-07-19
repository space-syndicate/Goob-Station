using Content.Shared.StatusIcon;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Imperial.CartridgeLoader.Cartridges;

[Serializable, NetSerializable]
public readonly record struct NanoChatContact(NetEntity Id, string Name, bool Visible, string? JobTitle, ProtoId<JobIconPrototype>? JobIconId);

[Serializable, NetSerializable]
public sealed class NanoChatMessage(NetEntity senderId, string senderName, string content, TimeSpan sendTime)
{
    public NetEntity SenderId = senderId;
    public string SenderName = senderName;
    public string Content = content;
    public TimeSpan SendTime = sendTime;
}

[Serializable, NetSerializable]
public sealed class NanoChatChat(int id, string name, NetEntity? owner, List<NetEntity> members, List<NanoChatMessage> messages, bool automated = true)
{
    public int Id = id;
    public string Name = name;
    public NetEntity? Owner = owner;
    public List<NetEntity> Members = members;
    public List<NanoChatMessage> Messages = messages;
    public bool Automated = automated;
}

[Serializable, NetSerializable]
public sealed class NanoChatBoundUserInterfaceState(
    bool notificationOn,
    NetEntity? currentUserId,
    NanoChatChat? currentChat,
    List<NanoChatChat> chats,
    List<NanoChatContact> contacts,
    bool isServerOnline,
    bool isContactReachable,
    bool canSendLocation,
    bool canPrint,
    bool finePrintText,
    Dictionary<int, int> unreadMessages,
    Dictionary<NetEntity, string> typingUsers)
    : BoundUserInterfaceState
{
    public bool NotificationOn = notificationOn;
    public NetEntity? CurrentUserId = currentUserId;
    public NanoChatChat? CurrentChat = currentChat;
    public List<NanoChatChat> Chats = chats;
    public List<NanoChatContact> Contacts = contacts;
    public bool IsServerOnline = isServerOnline;
    public bool IsContactReachable = isContactReachable;
    public bool CanSendLocation = canSendLocation;
    public bool CanPrint = canPrint;
    public bool FinePrintText = finePrintText;
    public Dictionary<int, int> UnreadMessages = unreadMessages;
    public Dictionary<NetEntity, string> TypingUsers = typingUsers;
}
