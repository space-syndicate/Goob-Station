using Content.Shared.Imperial.XenoGenetics;
using Robust.Shared.Prototypes;
using System.Linq;
using Content.Shared.Imperial.XenoGenetics.Genes.Components;
using Content.Shared.Imperial.XenoGenetics.Components;
using Content.Shared.Humanoid;

namespace Content.Shared.Imperial.XenoGenetics.Genes;

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
        EntityManager.AddComponents(args.Target, component.Components, false);
    }
    private void OnGeneWithdraw(EntityUid uid, AddComponentsNoRandomGeneComponent component, ref GeneWithdrawnEvent args)
    {
        EntityManager.RemoveComponents(args.Target, component.Components);
    }
}