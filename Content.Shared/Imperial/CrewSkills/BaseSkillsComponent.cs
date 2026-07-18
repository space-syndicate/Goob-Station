using Robust.Shared.Prototypes;

namespace Content.Shared.Imperial.CrewSkills;


public abstract partial class BaseSkillsComponent : Component
{
    /// <summary>
    /// Skills that the entity will have.
    /// </summary>
    [DataField, AutoNetworkedField]
    public HashSet<ProtoId<SkillPrototype>> Skills = new();

    /// <summary>
    /// Skill groups a person will have. Each skill in the group is been added.
    /// </summary>
    [DataField, AutoNetworkedField]
    public HashSet<ProtoId<SkillGroupPrototype>> Groups = new();

    /// <summary>
    /// Fake skills that will be shown when examining an entity
    /// </summary>
    [DataField, AutoNetworkedField]
    public HashSet<ProtoId<SkillPrototype>> FakeSkills = new();

    /// <summary>
    /// Fake skills that will be shown when examining an entity
    /// </summary>
    [DataField, AutoNetworkedField]
    public HashSet<ProtoId<SkillGroupPrototype>> FakeGroups = new();


    /// <summary>
    /// Currently, skills work in a gameplay way that they change certain things IN THE ABSENCE OF THE SKILL.
    /// That is, if we have a skill, it won't affect the mechanics, so we call SkillsRelayEvents on missing skills to have them change the mechanics.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public HashSet<ProtoId<SkillPrototype>> MissingSkills = new();

    /// <summary>
    /// These are all the player's skills obtained from the specified skill groups and fields.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public HashSet<ProtoId<SkillPrototype>> TotalSkills = new();

    /// <summary>
    /// Total fake skills
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public HashSet<ProtoId<SkillPrototype>> TotalFakeSkills = new();
}
