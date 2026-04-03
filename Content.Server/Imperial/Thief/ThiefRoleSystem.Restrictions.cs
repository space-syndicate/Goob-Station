using Content.Shared.Roles;
using Content.Shared.Roles.Components;
using Content.Server.Ame.Components;
using Content.Server.ParticleAccelerator.Components;
using Content.Shared.Popups;
using Content.Shared.Interaction.Events;
using Content.Shared.Mind;
using Content.Shared.Ghost;

namespace Content.Server.Imperial.Thief;
public sealed class ThiefRoleSystem : EntitySystem
{
    [Dependency] private readonly SharedRoleSystem _roles = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AmeControllerComponent, GettingInteractedWithAttemptEvent>(OnAmeInteractionAttempt);
        SubscribeLocalEvent<ParticleAcceleratorControlBoxComponent, GettingInteractedWithAttemptEvent>(OnPaInteractionAttempt);
    }

    private void OnAmeInteractionAttempt(Entity<AmeControllerComponent> ent, ref GettingInteractedWithAttemptEvent args)
    {
        CheckRestriction(ref args);
    }

    private void OnPaInteractionAttempt(Entity<ParticleAcceleratorControlBoxComponent> ent, ref GettingInteractedWithAttemptEvent args)
    {
        CheckRestriction(ref args);
    }

    private void CheckRestriction(ref GettingInteractedWithAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        if (HasComp<GhostComponent>(args.Uid))
            return;

        if (!_mind.TryGetMind(args.Uid, out var mindId, out _))
            return;

        if (!_roles.MindHasRole<ThiefRoleComponent>(mindId, out _))
            return;

        args.Cancelled = true;
        _popup.PopupEntity(Loc.GetString("thief-restriction-popup"), args.Uid, args.Uid, PopupType.LargeCaution);
    }
}
