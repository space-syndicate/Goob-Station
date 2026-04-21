using Content.Shared.Imperial.XenoGenetics;
using Robust.Shared.Prototypes;
using System.Linq;
using Content.Shared.Imperial.XenoGenetics.Genes.Components;
using Content.Shared.Imperial.XenoGenetics.Components;
using Content.Shared.NPC.Components;
using Content.Shared.NPC.Systems;
using Content.Shared.CombatMode.Pacification;

namespace Content.Shared.Imperial.XenoGenetics.Genes;

public sealed class RemoveFactionGeneSystem : EntitySystem
{
    [Dependency] private readonly NpcFactionSystem _npc = default!;
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RemoveFactionGeneComponent, ComponentInit>(OnAfterHandleState);
        SubscribeLocalEvent<RemoveFactionGeneComponent, ComponentShutdown>(OnComponentShutdown);
    }
    private void OnAfterHandleState(EntityUid uid, RemoveFactionGeneComponent component, ComponentInit args)
    {
        if (TryComp<NpcFactionMemberComponent>(uid, out var fact))
        {
            component.Factions = fact.Factions;
            
            RemComp<NpcFactionMemberComponent>(uid);
            component.Active = true;
        }

        if (HasComp<PacifiedComponent>(uid))
        {
            component.HadPacifist = true;
            return;
        }

         EnsureComp<PacifiedComponent>(uid);
    }
    private void OnComponentShutdown(EntityUid uid, RemoveFactionGeneComponent component, ComponentShutdown args)
    {
        if (component.Active == true)
        {
            var npcComp = EnsureComp<NpcFactionMemberComponent>(uid);
            _npc.AddFactions((uid, npcComp), component.Factions);
            component.Active = false;
        }

        if (!component.HadPacifist)
            RemComp<PacifiedComponent>(uid);
    }
}