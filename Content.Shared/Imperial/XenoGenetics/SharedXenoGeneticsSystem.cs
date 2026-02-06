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
using Robust.Shared.Random;
using Content.Shared.Alert;

namespace Content.Shared.Imperial.XenoGenetics;

public abstract class SharedXenoGeneticsSystem : EntitySystem
{
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly IRobustRandom _rand = default!;
    [Dependency] private readonly AlertsSystem _alertsSystem = default!;
    

    public override void Initialize()
    {
        base.Initialize();
        
        SubscribeLocalEvent<GeneSplicerComponent, AfterInteractEvent>(OnGeneSplicerInteract);

        SubscribeLocalEvent<XenoGeneComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<XenoGeneComponent, ComponentStartup>(OnXenoGeneStartup);

        SubscribeLocalEvent<XenoGeneComponent, GeneInsertedEvent>(OnGeneInsert);
        SubscribeLocalEvent<XenoGeneComponent, GeneWithdrawnEvent>(OnGeneWithdraw);
    }
    private void OnGeneInsert(EntityUid uid, XenoGeneComponent component, ref GeneInsertedEvent args)
    {
        _alertsSystem.ShowAlert(args.Target, "XenogeneInserted");
    }
    private void OnGeneWithdraw(EntityUid uid, XenoGeneComponent component, ref GeneWithdrawnEvent args)
    {
        _alertsSystem.ClearAlert(args.Target, "XenogeneInserted");
    }
    private void OnXenoGeneStartup(EntityUid uid, XenoGeneComponent component, ComponentStartup args)
    {

        if(component.randomizeGeneQuality)
        {
            float multiplier;
            int quality = _rand.Next(0, 10);
            switch(quality)
            {
                case <= 2:
                    multiplier = _rand.Next(1, 200) / 1000f;
                    break;
                case > 2 and <= 7:
                    multiplier = _rand.Next(200, 600) / 1000f;
                    break;
                case > 7 and <= 9:
                    multiplier = _rand.Next(600, 900) / 1000f;
                    break;
                case > 9 and <= 10:
                    multiplier = _rand.Next(900, 1200) / 1000f;
                    break;
                default:
                    multiplier = 0.1f;
                    break;
            }   

            component.geneMultiplier = multiplier;
        }
        
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