using Content.Shared.Imperial.XenoGenetics;
using Robust.Shared.Prototypes;
using System.Linq;
using Content.Shared.Imperial.XenoGenetics.Genes.Components;
using Content.Shared.Imperial.XenoGenetics.Components;
using Content.Shared.NPC.Components;
using Content.Shared.CombatMode.Pacification;

namespace Content.Shared.Imperial.XenoGenetics.Genes;

public sealed class RemoveFactionGeneSystem : EntitySystem
{
    private NpcFactionMemberComponent  _faction = new NpcFactionMemberComponent ();

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
            _faction = fact;
            RemComp<NpcFactionMemberComponent>(uid);
            component.Active = true;
        }
        if (!HasComp<PacifiedComponent>(uid))
            AddComp<PacifiedComponent>(uid);
    }
    private void OnComponentShutdown(EntityUid uid, RemoveFactionGeneComponent component, ComponentShutdown args)
    {
        if (component.Active == true)
        {
            AddComp(uid, _faction, true);
            component.Active = false;
        }
        if (HasComp<PacifiedComponent>(uid))
            RemComp<PacifiedComponent>(uid);
    }
}