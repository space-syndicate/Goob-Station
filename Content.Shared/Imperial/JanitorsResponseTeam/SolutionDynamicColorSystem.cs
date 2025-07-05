using Content.Shared.Imperial.JanitorsResponseTeam.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Paper;
using Content.Shared.Roles;
using Content.Shared.FixedPoint;
using Content.Shared.Mind;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using System.Diagnostics.CodeAnalysis;

namespace Content.Shared.Imperial.JanitorsResponseTeam;

public sealed class SolutionDynamicColorOfStampSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _protoManager = default!;
    [Dependency] private readonly SharedRoleSystem _roleSystem = default!;
    [Dependency] private readonly SharedMindSystem _mindSystem = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<SolutionDynamicColorOfStampComponent, SolutionContainerChangedEvent>(OnStampDynamicColor);
    }

    public void OnStampDynamicColor(EntityUid uid, SolutionDynamicColorOfStampComponent comp, ref SolutionContainerChangedEvent args)
    {
        var colorSolution = args.Solution.GetColor(_protoManager);

        if (args.Solution.Volume == FixedPoint2.Zero)
        {
            RemComp<StampComponent>(uid);
            return;
        }

        var stampComp = EnsureComp<StampComponent>(uid);
        stampComp.StampedColor = colorSolution;

        if (comp.CheckValidRole && comp.RoleName != null)
        {
            if (!TryGetItemOwner(uid, out var user))
                return;

            if (!IsUserDefiniteJob(user.Value, comp))
            {
                stampComp.StampedName = comp.FalseStampedName;
            }
            else
            {
                stampComp.StampedName = comp.TrueStampedName;
            }
        }
    }

    private bool IsUserDefiniteJob(EntityUid user, SolutionDynamicColorOfStampComponent comp)
    {
        if (string.IsNullOrEmpty(comp.RoleName))
            return false;
        if (string.IsNullOrEmpty(comp.FalseStampedName))
            return false;

        if (!_mindSystem.TryGetMind(user, out var mindId, out var _))
            return false;

        foreach (var role in _roleSystem.MindGetAllRoleInfo(mindId))
        {
            if (role.Name.Contains(comp.RoleName, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private bool TryGetItemOwner(EntityUid item, [NotNullWhen(true)] out EntityUid? owner)
    {
        owner = null;

        var query = EntityQueryEnumerator<MetaDataComponent, ContainerManagerComponent>();
        while (query.MoveNext(out var uid, out _, out var containerManager))
        {
            foreach (var container in containerManager.Containers.Values)
            {
                if (container.Contains(item))
                {
                    owner = uid;
                    return true;
                }
            }
        }

        return false;
    }
}

