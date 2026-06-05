using Content.Shared.Paper;

namespace Content.Shared._CorvaxGoob.FaxTracker;

public sealed class FaxMessageReceivedEvent : EntityEventArgs
{
    public readonly EntityUid Fax;
    public readonly string PaperName;
    public readonly string Content;
    public readonly List<StampDisplayInfo> Stamps;
    public readonly string? SenderAddress;
    public readonly string SenderName;

    public FaxMessageReceivedEvent(EntityUid fax, string paperName, string content, List<StampDisplayInfo> stamps, string? senderAddress, string senderName)
    {
        Fax = fax;
        PaperName = paperName;
        Content = content;
        Stamps = stamps;
        SenderAddress = senderAddress;
        SenderName = senderName;
    }
}
