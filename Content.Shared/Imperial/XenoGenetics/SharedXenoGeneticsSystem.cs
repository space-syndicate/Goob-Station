using Content.Shared.Imperial.XenoGenetics.Components;
using Content.Shared.Imperial.XenoGenetics.Genes.Components;
using Content.Shared.Imperial.XenoGenetics.Genes;
using Content.Shared.Examine;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Content.Shared.Mobs.Components;

namespace Content.Shared.Imperial.XenoGenetics;

public abstract class SharedXenoGeneticsSystem : EntitySystem
{
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GeneSplicerComponent, AfterInteractEvent>(OnGeneSplicerInteract);

        SubscribeLocalEvent<XenoGeneComponent, ExaminedEvent>(OnExamined);
    }

    private void OnExamined(EntityUid uid, XenoGeneComponent component, ref ExaminedEvent args)
    {
        args.PushMarkup(Loc.GetString("Качество гена: " + component.geneMultiplier * 100 + "%"));
    }

    private void OnGeneSplicerInteract(EntityUid uid, GeneSplicerComponent comp, AfterInteractEvent args)
    {
        if(args.Handled == true)
            return;
        if(args.Target == null)
            return;
        if(!TryComp<MobStateComponent>(args.Target, out var stateComp))
            return;
        switch (comp.InsertMode)
        {
            case GeneSplicerMode.Insert:
            _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager, args.User, TimeSpan.FromSeconds(5f), new GeneInsertingDoAfterEvent(), uid, target: args.Target, used: uid)
            {
                BreakOnMove = true,
                NeedHand = true,
            });
            break;

            case GeneSplicerMode.Withdraw:
            _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager, args.User, TimeSpan.FromSeconds(7.5f), new GeneWithdrawDoAfterEvent(), uid, target: args.Target, used: uid)
            {
                BreakOnMove = true,
                NeedHand = true,
            });
            break;

            default:
            break;
        }
    }
}
[Serializable, NetSerializable]
public sealed partial class GeneInsertingDoAfterEvent : SimpleDoAfterEvent
{
}
[Serializable, NetSerializable]
public sealed partial class GeneWithdrawDoAfterEvent : SimpleDoAfterEvent
{
}
[ByRefEvent]
public readonly record struct GeneInsertedEvent(EntityUid Gene, EntityUid Target);
[ByRefEvent]
public readonly record struct GeneWithdrawnEvent(EntityUid Gene, EntityUid Target);