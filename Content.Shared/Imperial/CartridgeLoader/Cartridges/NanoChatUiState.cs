using Robust.Shared.Serialization;

namespace Content.Shared.Imperial.CartridgeLoader.Cartridges;

[Serializable, NetSerializable]
public sealed class NanoChatMessage(string? sender, string content)
{
    public string? Sender = sender;
    public string Content = content;
}

[Serializable, NetSerializable]
public sealed class NanoChatBoundUserInterfaceState(
    bool notificationOn,
    string? currentContactName,
    string? pdaCardName,
    List<string> contacts,
    List<NanoChatMessage> messages)
    : BoundUserInterfaceState
{
    public bool NotificationOn = notificationOn;
    public string? CurrentContactName = currentContactName;
    public string? PdaCardName = pdaCardName;
    public List<string> Contacts = contacts;
    public List<NanoChatMessage> Messages = messages;
}
