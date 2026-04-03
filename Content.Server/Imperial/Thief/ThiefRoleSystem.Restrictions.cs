using Content.Shared.Roles;
using Content.Shared.Roles.Components;
using Content.Server.Ame.Components;
using Content.Server.ParticleAccelerator.Components;
using Content.Shared.Singularity.Components;
using Content.Shared.Popups;
using Content.Shared.Interaction.Events;
using Content.Shared.Mind;
using Content.Shared.Strip.Components;
using Content.Shared.Ghost;
using System.Reflection.Metadata;

namespace Content.Server.Imperial.Thief;
public sealed class ThiefRoleSystem : EntitySystem
{
    [Dependency] private readonly SharedRoleSystem _roles = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;



    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ThievingComponent, InteractionAttemptEvent>(OnInteractionAttempt);
    }

    private void OnInteractionAttempt(Entity<ThievingComponent> ent, ref InteractionAttemptEvent args)
    {
        var isBlocked = HasComp<AmeControllerComponent>(args.Target) ||
                        HasComp<ParticleAcceleratorControlBoxComponent>(args.Target) ||
                        HasComp<EmitterComponent>(args.Target) ||
                        HasComp<ContainmentFieldGeneratorComponent>(args.Target);

        if (isBlocked && !HasComp<GhostComponent>(args.Uid))
        {
            if (!_mind.TryGetMind(args.Uid, out var mindId, out _))
                return;

            if (!_roles.MindHasRole<ThiefRoleComponent>(mindId, out _))
                return;

            _popup.PopupEntity(Loc.GetString("thief-restriction-popup"), args.Uid, PopupType.LargeCaution);
            args.Cancelled = true;
        }
    }
}
