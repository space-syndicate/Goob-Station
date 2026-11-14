using Lidgren.Network;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared._BlackM.DiscordAuth;

/// <summary>
/// Server sends this event to client on connect if Discord auth is required.
/// </summary>
public sealed class MsgDiscordAuthRequired : NetMessage
{
    public override MsgGroups MsgGroup => MsgGroups.Command;

    public string LinkCode = string.Empty;

    public bool ShowWindow;

    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
    {
        LinkCode = buffer.ReadString();
        ShowWindow = buffer.ReadBoolean();
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
    {
        buffer.Write(LinkCode);
        buffer.Write(ShowWindow);
    }
}