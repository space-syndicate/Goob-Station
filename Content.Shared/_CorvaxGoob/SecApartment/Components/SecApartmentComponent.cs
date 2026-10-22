using Content.Shared.Medical.SuitSensor;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._CorvaxGoob.SecApartment;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SecApartmentComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid? Station;

    [DataField]
    public string SecurityDepartment = "Security";
}

[Serializable, NetSerializable]
public sealed class Squad(string squadId, string name)
{
    public string SquadId { get; set; } = squadId;
    public string Name { get; set; } = name;
    public string Description { get; set; } = string.Empty;
    public List<CrewMemberInfo> Members { get; set; } = new();
    public SquadStatus Status { get; set; } = SquadStatus.Active;
    public SquadIconNum IconId { get; set; } = SquadIconNum.Alpha;
}

[Serializable, NetSerializable]
public sealed class CrewMemberInfo(
    string memberId,
    NetEntity? ownerUid,
    string name,
    string jobTitle,
    string jobIcon,
    SuitSensorStatus? suitSensor)
{
    public string MemberId { get; } = memberId;
    public NetEntity? OwnerUid { get; set; } = ownerUid;
    public string Name { get; } = name;
    public string JobTitle { get; } = jobTitle;
    public string JobIcon { get; } = jobIcon;
    public SuitSensorStatus? SensorStatus { get; } = suitSensor;
}

[Serializable, NetSerializable]
public sealed class TimerEntry(
    NetEntity timerUid,
    string label,
    TimeSpan remainingTime,
    TimeSpan totalTime,
    TimeSpan? finishedAt = null)
{
    public NetEntity TimerUid { get; set; } = timerUid;
    public string Label { get; set; } = label;
    public TimeSpan RemainingTime { get; set; } = remainingTime;
    public TimeSpan TotalTime { get; set; } = totalTime;
    public TimeSpan? FinishedAt { get; set; } = finishedAt;
}
