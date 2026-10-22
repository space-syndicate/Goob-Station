using Content.Shared.Mind;
using Robust.Shared.Configuration;

namespace Content.Shared._CorvaxGoob.Skills;

public abstract class SharedSkillsSystem : EntitySystem
{
    public abstract bool HasSkill(EntityUid entity, Skills skill);
}
