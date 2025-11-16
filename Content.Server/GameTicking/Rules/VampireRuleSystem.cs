using Content.Server.GameTicking.Rules.Components;
using Content.Server.Roles;
using Content.Shared.GameTicking.Components;
using Content.Shared.Roles;
using Content.Shared.Roles.Components;

namespace Content.Server.GameTicking.Rules;

public sealed class VampireRuleSystem : GameRuleSystem<VampireRuleComponent>
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VampireRoleComponent, GetBriefingEvent>(OnGetBriefing);
    }

    private void OnGetBriefing(Entity<VampireRoleComponent> role, ref GetBriefingEvent args)
    {
        args.Append(Loc.GetString("vampire-role-greeting-human"));
    }

    protected override void AppendRoundEndText(EntityUid uid,
        VampireRuleComponent component,
        GameRuleComponent gameRule,
        ref RoundEndTextAppendEvent args)
    {
        base.AppendRoundEndText(uid, component, gameRule, ref args);
    }
}
