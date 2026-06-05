using Content.Shared.Paper;
using Robust.Shared.Serialization;

namespace Content.Shared._CorvaxGoob.FaxTracker;

[Serializable, NetSerializable]
public sealed class FaxTrackerUiState : BoundUserInterfaceState
{
    public List<FaxTrackerEntry> Entries;
    public bool NotificationsOn;
    public bool Bound;
    public string? FaxName;

    public List<FaxTrackerSender> Blacklist;
    public List<FaxTrackerSender> KnownFaxes;

    public FaxTrackerUiState(List<FaxTrackerEntry> entries, bool notificationsOn, bool bound, string? faxName, List<FaxTrackerSender> blacklist, List<FaxTrackerSender> knownFaxes)
    {
        Entries = entries;
        NotificationsOn = notificationsOn;
        Bound = bound;
        FaxName = faxName;
        Blacklist = blacklist;
        KnownFaxes = knownFaxes;
    }
}

[Serializable, NetSerializable]
public struct FaxTrackerEntry
{
    public int Number;
    public string? Title;
    public string Content;
    public List<StampDisplayInfo> Stamps;
    public TimeSpan Time;
    public bool IsFavorite;
    public string SenderName;

    public FaxTrackerEntry(int number, string? title, string content, List<StampDisplayInfo> stamps, TimeSpan time, string senderName)
    {
        Number = number;
        Title = title;
        Content = content;
        Stamps = stamps;
        Time = time;
        IsFavorite = false;
        SenderName = senderName;
    }
}

[Serializable, NetSerializable]
public struct FaxTrackerSender
{
    public string Address;
    public string Name;

    public FaxTrackerSender(string address, string name)
    {
        Address = address;
        Name = name;
    }
}
