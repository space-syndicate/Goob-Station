using Content.Shared.Imperial.CrewSkills;
using Robust.Shared.Prototypes;

namespace Content.Shared.Imperial.Helpers;


public sealed class SkillsHelper
{
    public static readonly ProtoId<SkillPrototype> ShootingSkill = "Shooting";


    public static IEnumerable<ProtoId<SkillPrototype>> GetGroupsSkills(IEnumerable<ProtoId<SkillGroupPrototype>> groups, IPrototypeManager prototypeManager)
    {
        return [];
    }

    public static IEnumerable<ProtoId<SkillPrototype>> GetGroupSkills(ProtoId<SkillGroupPrototype> group, IPrototypeManager prototypeManager)
    {
        return [];
    }
}
