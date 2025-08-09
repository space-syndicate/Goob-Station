using Content.Shared.GameTicking;

namespace Content.Shared._CorvaxGoob.Skills;

public abstract class SharedSkillsSystem : EntitySystem
{
    public bool Enabled { get; set; } = true;

    public override void Initialize()
    {
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestartCleanup);
    }

    private void OnRoundRestartCleanup(ref RoundRestartCleanupEvent e)
    {
        Enabled = true;
    }

    public bool HasSkill(EntityUid entity, Skills skill, SkillsComponent? component = null)
    {
        if (!Enabled)
            return true;

        if (!Resolve(entity, ref component, false))
            return false;

        return component.Skills.Contains(skill);
    }

    public void GrantAllSkills(EntityUid entity, SkillsComponent? component = null)
    {
        component ??= EnsureComp<SkillsComponent>(entity);

        component.Skills.UnionWith(Enum.GetValues<Skills>());

        Dirty(entity, component);
    }
}
