using Content.Shared.Medical.SuitSensor;
using Content.Shared.SecApartment;
using Robust.Shared.Serialization;

namespace Content.Shared._CorvaxGoob.SecApartment;

[Serializable, NetSerializable]
public enum SecApartmentUiKey : byte
{
    Key
}

[Serializable, NetSerializable]
public sealed class SecApartmentUpdateState(
    string stationName,
    List<CrewMemberInfo> securityCrew,
    List<CrewMemberInfo> unassignedSecurity,
    List<Squad> squads) : BoundUserInterfaceState
{
    public string StationName { get; } = stationName;
    public List<CrewMemberInfo> SecurityCrew { get; } = securityCrew;
    public List<CrewMemberInfo> UnassignedSecurity { get; } = unassignedSecurity;
    public List<Squad> Squads { get; } = squads;
}

[Serializable, NetSerializable]
public sealed class SensorStatusUpdateState(
    Dictionary<string, SuitSensorStatus?> memberStatuses,
    Dictionary<string, (string Location, bool HasLocation)> squadLocations)
    : BoundUserInterfaceState
{
    public Dictionary<string, SuitSensorStatus?> MemberStatuses { get; } = memberStatuses;
    public Dictionary<string, (string Location, bool HasLocation)> SquadLocations { get; } = squadLocations;
}

[Serializable, NetSerializable]
public sealed class CreateSquadMessage(string squadName) : BoundUserInterfaceMessage
{
    public string SquadName { get; } = squadName;
}

[Serializable, NetSerializable]
public sealed class DeleteSquadMessage(string squadId) : BoundUserInterfaceMessage
{
    public string SquadId { get; } = squadId;

}

[Serializable, NetSerializable]
public sealed class RenameSquadMessage(string squadId, string newName) : BoundUserInterfaceMessage
{
    public string SquadId { get; } = squadId;
    public string NewName { get; } = newName;

}

[Serializable, NetSerializable]
public sealed class UpdateSquadDescriptionMessage(string squadId, string description)
    : BoundUserInterfaceMessage
{
    public string SquadId { get; } = squadId;
    public string Description { get; } = description;

}

[Serializable, NetSerializable]
public sealed class AddMemberToSquadMessage(string squadId, string memberId) : BoundUserInterfaceMessage
{
    public string SquadId { get; } = squadId;
    public string MemberId { get; } = memberId;

}

[Serializable, NetSerializable]
public sealed class RemoveMemberFromSquadMessage(string squadId, string memberId) : BoundUserInterfaceMessage
{
    public string SquadId { get; } = squadId;
    public string MemberId { get; } = memberId;

}

[Serializable, NetSerializable]
public sealed class ChangeSquadIconMessage(string squadId, SquadIconNum iconId) : BoundUserInterfaceMessage
{
    public string SquadId { get; } = squadId;
    public SquadIconNum IconId { get; } = iconId;

}

[Serializable, NetSerializable]
public sealed class ChangeSquadStatusMessage(string squadId, SquadStatus status) : BoundUserInterfaceMessage
{
    public string SquadId { get; } = squadId;
    public SquadStatus Status { get; } = status;
}

[Serializable, NetSerializable]
public sealed class TimerUpdateState(List<TimerEntry> timers) : BoundUserInterfaceState
{
    public List<TimerEntry> Timers { get; } = timers;

}

[Serializable, NetSerializable]
public sealed class RemoveTimerMessage(NetEntity timerUid) : BoundUserInterfaceMessage
{
    public NetEntity TimerUid { get; } = timerUid;
}
