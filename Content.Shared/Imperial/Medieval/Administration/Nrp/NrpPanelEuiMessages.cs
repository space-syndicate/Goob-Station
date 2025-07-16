using Content.Shared.Eui;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared.Imperial.Medieval.Administration.Nrp;

[Serializable, NetSerializable]
public sealed class NrpMessagesRequest : EuiMessageBase;

[Serializable, NetSerializable]
public sealed class NrpMessagesResponse : EuiMessageBase
{
    public List<NrpMessage> Messages { get; }
    public NrpMessagesResponse(List<NrpMessage> messages)
    {
        Messages = messages;
    }
}

[Serializable, NetSerializable]
public sealed class NewNrpMessageMsg : EuiMessageBase
{
    public NrpMessage Message { get; }
    public NewNrpMessageMsg(NrpMessage message)
    {
        Message = message;
    }
}

[Serializable, NetSerializable]
public sealed class NrpMessage : EuiMessageBase
{
    public Guid Id { get; } = Guid.NewGuid();
    public string UnformattedMessage { get; }
    public Dictionary<string, bool> BannedWords { get; }
    public string Message { get; }
    public string PlayerName { get; }
    public NetUserId PlayerId { get; }
    public NetEntity? PlayerAttachedEntity { get; }
    public string EntityName { get; }
    public string? JobName { get; }
    public int Violations { get; }
    public NrpMessage(string unformattedMessage, Dictionary<string, bool> bannedWords, string message, string playerName, NetUserId playerId, NetEntity? playerAttachedEntity, string entityName, string? jobName, int violations)
    {
        UnformattedMessage = unformattedMessage;
        BannedWords = bannedWords;
        Message = message;
        PlayerId = playerId;
        PlayerName = playerName;
        PlayerAttachedEntity = playerAttachedEntity;
        EntityName = entityName;
        JobName = jobName;
        Violations = violations;
    }

    public override bool Equals(object? obj) => Equals(obj as NrpMessage);

    public bool Equals(NrpMessage? other)
    {
        if (ReferenceEquals(this, other))
            return true;
        if (other is null)
            return false;

        return Id == other.Id;
    }

    public override int GetHashCode() => Id.GetHashCode();

    public static bool operator ==(NrpMessage? left, NrpMessage? right) =>
        EqualityComparer<NrpMessage>.Default.Equals(left, right);

    public static bool operator !=(NrpMessage? left, NrpMessage? right) => !(left == right);
}

[Serializable, NetSerializable]
public sealed class ResolveNrpMessageMsg : EuiMessageBase
{
    public NrpMessage Message { get; }
    public bool IsNrp { get; }
    public ResolveNrpMessageMsg(NrpMessage message, bool isNrp)
    {
        Message = message;
        IsNrp = isNrp;
    }
}

[Serializable, NetSerializable]
public sealed class RemoveNrpMessageMsg : EuiMessageBase
{
    public NrpMessage Message { get; }
    public RemoveNrpMessageMsg(NrpMessage message)
    {
        Message = message;
    }
}

[Serializable, NetSerializable]
public sealed class NrpStatsRequest : EuiMessageBase;

[Serializable, NetSerializable]
public sealed class NrpStatsResponse : EuiMessageBase
{
    public Dictionary<string, int> Entries { get; }
    public Dictionary<string, int> RoundEntries { get; }
    public NrpStatsResponse(Dictionary<string, int> entries, Dictionary<string, int> roundEntries)
    {
        Entries = entries;
        RoundEntries = roundEntries;
    }
}
