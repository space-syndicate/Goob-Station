using Robust.Shared.Prototypes;

namespace Content.Shared.Imperial.CrewSkills;


[Prototype]
public sealed partial class SkillGroupPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public LocId Name;

    [DataField]
    public List<ProtoId<SkillPrototype>> Skills = new();

    /// <summary>
    /// Whether to show this group when inspecting a person.
    /// Useful when creating a skill group (for example, for an antagonist) that includes each skill, but this group should be divided into subgroups.
    /// </summary>
    [DataField]
    public bool ShowInUI = false;

    /// <summary>
    /// The higher this value, the higher the group will be during the examination.
    /// </summary>
    [DataField]
    public int Priority = 0;
}
