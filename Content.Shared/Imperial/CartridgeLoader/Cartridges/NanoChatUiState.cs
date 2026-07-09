using Robust.Shared.Serialization;

namespace Content.Shared.Imperial.CartridgeLoader.Cartridges;

[Serializable, NetSerializable]
public record struct NanoChatContact(string Name, string? JobTitle);

[Serializable, NetSerializable]
public sealed class NanoChatMessage(string? sender, string content)
{
    public string? Sender = sender;
    public string Content = content;
}

[Serializable, NetSerializable]
public sealed class NanoChatBoundUserInterfaceState(
    bool notificationOn,
    NanoChatContact? currentContactName,
    string? pdaCardName,
    List<NanoChatContact> contacts,
    List<NanoChatMessage> messages)
    : BoundUserInterfaceState
{
    public bool NotificationOn = notificationOn;
    public NanoChatContact? CurrentContactName = currentContactName;
    public string? PdaCardName = pdaCardName;
    public List<NanoChatContact> Contacts = contacts;
    public List<NanoChatMessage> Messages = messages;
}
