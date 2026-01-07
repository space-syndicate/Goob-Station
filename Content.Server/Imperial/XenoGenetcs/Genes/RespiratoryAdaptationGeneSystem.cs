using Content.Shared.Imperial.XenoGenetics;
using Robust.Shared.Prototypes;
using System.Linq;
using Content.Shared.Imperial.XenoGenetics.Genes.Components;
using Content.Shared.Imperial.XenoGenetics.Components;
using Content.Server.Body.Components;
using Content.Server.Body.Systems;
using System.Diagnostics.CodeAnalysis;

namespace Content.Server.Imperial.XenoGenetics.Genes;///UpdateSaturation

public sealed class RespiratoryAdaptationGeneSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RespiratoryAdaptationGeneComponent, GeneInsertedEvent>(OnRespiratorGeneInsert);
        SubscribeLocalEvent<RespiratoryAdaptationGeneComponent, GeneWithdrawnEvent>(OnRespiratorGeneInsert);
    }
    private void OnRespiratorGeneInsert(EntityUid uid, RespiratoryAdaptationGeneComponent component, ref GeneInsertedEvent args)
    {
        var geneComp = EnsureComp<XenoGeneComponent>(args.Gene);
        component.Target = args.Target;
        if(geneComp.geneMultiplier * 100f > component.saturationThreshold)
        {
            if(!TryComp<RespiratorComponent>(args.Target, out var respirator))
                component.works = false;
        }
    }
    private void OnRespiratorGeneInsert(EntityUid uid, RespiratoryAdaptationGeneComponent component, ref GeneWithdrawnEvent args)
    {
        component.works = false;
    }
}