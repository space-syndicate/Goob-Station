using Content.Shared.Imperial.XenoGenetics;
using Robust.Shared.Prototypes;
using System.Linq;
using Content.Shared.Imperial.XenoGenetics.Genes.Components;
using Content.Shared.Imperial.XenoGenetics.Components;
using Content.Shared.Humanoid;
using Microsoft.VisualBasic;

namespace Content.Shared.Imperial.XenoGenetics.Genes;

public sealed class AddComponentsGeneSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<AddComponentsGeneComponent, GeneInsertedEvent>(OnGeneInsert);
        SubscribeLocalEvent<AddComponentsGeneComponent, GeneWithdrawnEvent>(OnGeneWithdraw);
    }
    private void OnGeneInsert(EntityUid uid, AddComponentsGeneComponent component, ref GeneInsertedEvent args)
    {
        var geneComp = EnsureComp<XenoGeneComponent>(args.Gene);
        if(geneComp.GeneMultiplier * 100f <= component.Threshold)
            return;
        EntityManager.AddComponents(args.Target, component.Components, true);
    }
    private void OnGeneWithdraw(EntityUid uid, AddComponentsGeneComponent component, ref GeneWithdrawnEvent args)
    {
        var geneComp = EnsureComp<XenoGeneComponent>(args.Gene);
        if(geneComp.GeneMultiplier * 100f <= component.Threshold)
            return;
        EntityManager.RemoveComponents(args.Target, component.Components);
    }
}

public sealed class AddComponentsNoRandomGeneSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<AddComponentsNoRandomGeneComponent, GeneInsertedEvent>(OnGeneInsert);
        SubscribeLocalEvent<AddComponentsNoRandomGeneComponent, GeneWithdrawnEvent>(OnGeneWithdraw);
    }
    private void OnGeneInsert(EntityUid uid, AddComponentsNoRandomGeneComponent component, ref GeneInsertedEvent args)
    {
        EntityManager.AddComponents(args.Target, component.Components, true);
    }
    private void OnGeneWithdraw(EntityUid uid, AddComponentsNoRandomGeneComponent component, ref GeneWithdrawnEvent args)
    {
        EntityManager.RemoveComponents(args.Target, component.Components);
    }
}