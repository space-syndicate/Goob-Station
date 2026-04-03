using Content.Server.Antag;
using Content.Server.Antag.Components;
using Content.Server.Traitor.Components;
using Content.Shared.Mind.Components;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Content.Shared.CombatMode.Pacification;
using Content.Shared.Roles;
using Content.Shared.Roles.Components;
using Content.Shared.Mind;
using System.Reflection.Metadata;
using System.Security.Cryptography;

namespace Content.Server.Imperial.Thief;
public sealed class AutoPacifiedSystem : EntitySystem
{
    [Dependency] private readonly SharedRoleSystem _roles = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MindAddedMessage>(OnMindAdded);
    }

    private void OnMindAdded(MindAddedMessage args)
    {
        if (_roles.MindHasRole<ThiefRoleComponent>((args.Mind.Owner, args.Mind.Comp), out _))
        {
            EnsureComp<PacifiedComponent>(args.Container);
        }
    }
}
