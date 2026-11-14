using Content.Shared.Access;
using Content.Shared.Radio;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Shared._BlackM.Announcement;

[Prototype("announcementChannel")]
public sealed partial class AnnouncementChannelPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public LocId Name;

    [DataField]
    public LocId Title = "blackm-announce-title-default";

    [DataField]
    public Color Accent = Color.FromHex("#9fb4cc");

    [DataField]
    public bool Everyone;

    [DataField]
    public List<ProtoId<DepartmentPrototype>> Departments = new();

    [DataField]
    public List<ProtoId<JobPrototype>> Jobs = new();

    [DataField]
    public List<ProtoId<RadioChannelPrototype>> RadioChannels = new();

    [DataField]
    public List<ProtoId<AccessLevelPrototype>> RequiredAccess = new();
}
