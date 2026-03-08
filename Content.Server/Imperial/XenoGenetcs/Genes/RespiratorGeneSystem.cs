using Content.Shared.Imperial.XenoGenetics;
using Robust.Shared.Prototypes;
using System.Linq;
using Content.Shared.Imperial.XenoGenetics.Genes.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Imperial.XenoGenetics.Components;
using Content.Server.Body.Components;

namespace Content.Server.Imperial.XenoGenetics.Genes;

public sealed class RespiratorGeneSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RespiratorGeneComponent, GeneInsertedEvent>(OnGeneInsert);
        SubscribeLocalEvent<RespiratorGeneComponent, GeneWithdrawnEvent>(OnGeneWithdraw);
    }
    private void OnGeneInsert(EntityUid uid, RespiratorGeneComponent component, ref GeneInsertedEvent args)
    {
        if(!TryComp<RespiratorComponent>(args.Target, out var resp))
            return;

        var gene = EnsureComp<XenoGeneComponent>(args.Gene);
        if(gene.GeneMultiplier * 100f >= component.GeneNeeded)
        {
            RemComp<RespiratorComponent>(args.Target);
            component.Active = true;
        }

    }
    private void OnGeneWithdraw(EntityUid uid, RespiratorGeneComponent component, ref GeneWithdrawnEvent args)
    {    
        if(component.Active == true)
        {
            EntityManager.AddComponents(args.Target, component.Components, false);
            component.Active = false;
        }
        
    }
        
}