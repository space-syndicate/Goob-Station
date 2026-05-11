using SkillTypes = Content.Shared._CorvaxGoob.Skills;

namespace Content.Client._CorvaxGoob.Skills;

public sealed partial class SkillsSystem : SkillTypes.SharedSkillsSystem
{
    public override bool HasSkill(EntityUid uid, SkillTypes.Skills skill) => true;
}
