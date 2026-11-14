using Robust.Shared.Serialization;

namespace Content.Shared._BlackM.Announcement;

[Serializable, NetSerializable]
public enum AnnouncementConsoleUiKey : byte
{
    Key
}

[Serializable, NetSerializable]
public sealed class AnnouncementChannelEntry
{
    public string Id;
    public string Name;
    public Color Accent;
    public bool Allowed;

    public AnnouncementChannelEntry(string id, string name, Color accent, bool allowed)
    {
        Id = id;
        Name = name;
        Accent = accent;
        Allowed = allowed;
    }
}

[Serializable, NetSerializable]
public sealed class AnnouncementConsoleBuiState : BoundUserInterfaceState
{
    public List<AnnouncementChannelEntry> Channels;
    public TimeSpan NextAnnouncement;
    public int MaxLength;
    public string SenderName;
    public string SenderJob;
    public bool HasAccess;

    public AnnouncementConsoleBuiState(
        List<AnnouncementChannelEntry> channels,
        TimeSpan nextAnnouncement,
        int maxLength,
        string senderName,
        string senderJob,
        bool hasAccess)
    {
        Channels = channels;
        NextAnnouncement = nextAnnouncement;
        MaxLength = maxLength;
        SenderName = senderName;
        SenderJob = senderJob;
        HasAccess = hasAccess;
    }
}

[Serializable, NetSerializable]
public sealed class AnnouncementConsoleSendMessage : BoundUserInterfaceMessage
{
    public string Channel;
    public string Text;

    public AnnouncementConsoleSendMessage(string channel, string text)
    {
        Channel = channel;
        Text = text;
    }
}

[Serializable, NetSerializable]
public sealed class AnnouncementBroadcastEvent : EntityEventArgs
{
    public NetEntity? Speaker;
    public string Sender;
    public string Title;
    public string Text;
    public Color Accent;
    public float Duration;

    public AnnouncementBroadcastEvent(NetEntity? speaker, string sender, string title, string text, Color accent, float duration)
    {
        Speaker = speaker;
        Sender = sender;
        Title = title;
        Text = text;
        Accent = accent;
        Duration = duration;
    }
}
