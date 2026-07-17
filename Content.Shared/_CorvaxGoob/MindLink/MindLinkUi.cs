using Robust.Shared.Serialization;

namespace Content.Shared._CorvaxGoob.MindLink;

[Serializable, NetSerializable]
public enum MindLinkUiKey : byte
{
    Key,
}

[Serializable, NetSerializable]
public sealed class MindLinkTarget(NetEntity entity, string name)
{
    public readonly NetEntity Entity = entity;
    public readonly string Name = name;
}

[Serializable, NetSerializable]
public sealed class MindLinkBuiState(
    List<MindLinkTarget> targets,
    string? recipientName,
    bool isReply)
    : BoundUserInterfaceState
{
    public readonly List<MindLinkTarget> Targets = targets;
    public readonly string? RecipientName = recipientName;
    public readonly bool IsReply = isReply;
}

[Serializable, NetSerializable]
public sealed class SelectMindLinkTargetMessage(NetEntity target) : BoundUserInterfaceMessage
{
    public readonly NetEntity Target = target;
}

[Serializable, NetSerializable]
public sealed class SendMindLinkMessage(string message) : BoundUserInterfaceMessage
{
    public readonly string Message = message;
}
