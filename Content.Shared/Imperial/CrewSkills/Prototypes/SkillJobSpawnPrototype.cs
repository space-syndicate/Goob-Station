using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Shared.Imperial.CrewSkills;


[Prototype]
public sealed partial class SkillJobSpawnPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public ProtoId<JobPrototype> AssignedJob;

    [DataField]
    public HashSet<ProtoId<SkillPrototype>> MindSkills = new();

    [DataField]
    public HashSet<ProtoId<SkillGroupPrototype>> MindGroups = new();

    [DataField]
    public HashSet<ProtoId<SkillPrototype>> BodySkills = new();

    [DataField]
    public HashSet<ProtoId<SkillGroupPrototype>> BodyGroups = new();
}
