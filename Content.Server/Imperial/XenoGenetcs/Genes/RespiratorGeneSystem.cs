using Content.Shared.Imperial.XenoGenetics;
using Robust.Shared.Prototypes;
using System.Linq;
using Content.Shared.Imperial.XenoGenetics.Genes.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Imperial.XenoGenetics.Components;
using Content.Server.Body.Components;

namespace Content.Server.Imperial.XenoGenetics.Genes;

public sealed class SpeedModifierGeneSystem : EntitySystem
{
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeedModifier = default!;
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
        if(gene.geneMultiplier * 100f >= component.geneNeeded)
        {
            RemComp<RespiratorComponent>(args.Target);
            component.active = true;
        }

    }
    private void OnGeneWithdraw(EntityUid uid, RespiratorGeneComponent component, ref GeneWithdrawnEvent args)
    {    
        if(component.active == true)
        {
            EntityManager.AddComponents(args.Target, component.Components, false);
            component.active = false;
        }
        
    }
        
}