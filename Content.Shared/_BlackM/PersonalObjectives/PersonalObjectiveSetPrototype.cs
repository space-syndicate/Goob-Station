using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Shared._BlackM.PersonalObjectives;

[Prototype("blackmPersonalObjectiveSet")]
public sealed partial class BlackMPersonalObjectiveSetPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public List<ProtoId<JobPrototype>> Jobs = new();

    [DataField]
    public List<EntProtoId> Always = new();

    [DataField]
    public List<EntProtoId> Pool = new();

    [DataField]
    public int Count = 2;
}
