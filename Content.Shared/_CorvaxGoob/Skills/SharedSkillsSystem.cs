using CVars = Content.Shared._CorvaxGoob.CCCVars.CCCVars;
using Content.Shared.Mind;
using Robust.Shared.Configuration;

namespace Content.Shared._CorvaxGoob.Skills;

public abstract class SharedSkillsSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;

    private bool _skillsEnabled = true;

    public override void Initialize()
    {
        base.Initialize();

        _skillsEnabled = _cfg.GetCVar(CVars.SkillsEnabled);
        Subs.CVar(_cfg, CVars.SkillsEnabled, value => _skillsEnabled = value);
    }

    public bool IsSkillsEnabled()
    {
        return _skillsEnabled;
    }

    /// <summary>
    /// Check does entity has current skill
    /// </summary>
    /// <param name="entity"></param>
    /// <param name="skill"></param>
    /// <returns>true if has, else false</returns>
    public bool HasSkill(EntityUid entity, Skills skill)
    {
        if (!_skillsEnabled)
            return true;

        if (HasComp<IgnoreSkillsComponent>(entity))
            return true;

        if (!_mind.TryGetMind(entity, out _, out var mind))
            return false;

        if (mind.Skills.Contains(Skills.All))
            return true;

        return mind.Skills.Contains(skill);
    }
}
