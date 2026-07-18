using Robust.Shared.Prototypes;

namespace Content.Shared.Imperial.CrewSkills;


[Prototype]
public sealed partial class SkillPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public LocId Name;

    [DataField("effects")]
    public BaseSkillEffect[] SkillEffects = [];

    /// <summary>
    /// Color used in UI
    /// </summary>
    [DataField]
    public Color Color = Color.White;
}
