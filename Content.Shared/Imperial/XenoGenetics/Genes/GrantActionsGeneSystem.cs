using Content.Shared.Imperial.XenoGenetics;
using Robust.Shared.Prototypes;
using System.Linq;
using Content.Shared.Imperial.XenoGenetics.Genes.Components;
using Content.Shared.Imperial.XenoGenetics.Components;
using Content.Shared.Actions.Components;
using Content.Shared.Actions;
using System.Numerics;

namespace Content.Shared.Imperial.XenoGenetics.Genes;

public sealed class GrantActionsGeneSystem : EntitySystem
{

    [Dependency] private readonly SharedActionsSystem _aSyst = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<GrantActionsGeneComponent, GeneInsertedEvent>(OnGeneInsert);
        SubscribeLocalEvent<GrantActionsGeneComponent, GeneWithdrawnEvent>(OnGeneWithdraw);
    }
    private void OnGeneInsert(EntityUid uid, GrantActionsGeneComponent component, GeneInsertedEvent args)
    {
        var xenoGeneComp = EnsureComp<XenoGeneComponent>(uid);

        if(xenoGeneComp.GeneMultiplier < component.Threshold)
            return;

        foreach (var actionProto in component.ActionsList)
        {
            EntityUid? actionEntity = null;
            _aSyst.AddAction(args.Target, ref actionEntity, actionProto);

            if (actionEntity != null)
                component.EntityList.Add(actionEntity.Value);
        }
        if (component.RandomCooldown)
        {
            foreach (var actionEnt2 in component.EntityList)
            {
                if (!TryComp<ActionComponent>(actionEnt2, out var actionComp))
                    continue;

                if (actionComp.UseDelay == null)
                    continue;

                _aSyst.SetUseDelay((actionEnt2, actionComp), TimeSpan.FromSeconds((float)actionComp.UseDelay.Value.TotalSeconds * (component.BasicModifier - xenoGeneComp.GeneMultiplier)));
            }
        }
    }
    private void OnGeneWithdraw(EntityUid uid, GrantActionsGeneComponent component, GeneWithdrawnEvent args)
    {
        var xenoGeneComp = EnsureComp<XenoGeneComponent>(uid);

        if(xenoGeneComp.GeneMultiplier < component.Threshold)
            return;

        foreach (var actionEntity in component.EntityList)
        {
            _aSyst.RemoveAction(args.Target, actionEntity);
        }
    }
}