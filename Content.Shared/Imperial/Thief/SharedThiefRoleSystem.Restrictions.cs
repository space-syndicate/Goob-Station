using Content.Shared.Singularity.Components;
using Content.Shared.Popups;
using Content.Shared.Interaction.Events;
using Content.Shared.Mind;
using Content.Shared.Ghost;
using Content.Shared.Roles;
using Content.Shared.Roles.Components;

namespace Content.Shared.Imperial.Thief;
public sealed class SharedThiefRoleSystem : EntitySystem
{
    [Dependency] private readonly SharedRoleSystem _roles = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EmitterComponent, GettingInteractedWithAttemptEvent>(GettingInteractedWithEmitterAttempt);
        SubscribeLocalEvent<ContainmentFieldGeneratorComponent, GettingInteractedWithAttemptEvent>(GettingInteractedWithCfgAttempt);
    }

    private void GettingInteractedWithEmitterAttempt(Entity<EmitterComponent> ent, ref GettingInteractedWithAttemptEvent args)
    {
        if (CheckRestriction(ref args))
        {
            args.Cancelled = true;
            _popup.PopupClient(Loc.GetString("thief-restriction-popup"), args.Uid, args.Uid, PopupType.LargeCaution);
        }
    }

    private void GettingInteractedWithCfgAttempt(Entity<ContainmentFieldGeneratorComponent> ent, ref GettingInteractedWithAttemptEvent args)
    {
        if (CheckRestriction(ref args))
        {
            args.Cancelled = true;
            _popup.PopupClient(Loc.GetString("thief-restriction-popup"), args.Uid, args.Uid, PopupType.LargeCaution);
        }
    }

    public bool CheckRestriction(ref GettingInteractedWithAttemptEvent args)
    {
        if (args.Cancelled)
            return false;

        if (HasComp<GhostComponent>(args.Uid))
            return false;

        if (!_mind.TryGetMind(args.Uid, out var mindId, out _))
            return false;

        if (!_roles.MindHasRole<ThiefRoleComponent>(mindId, out _))
            return false;

        return true;
    }
}
