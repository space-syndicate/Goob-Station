using Robust.Shared.Prototypes;
using System.Linq;
using Content.Server.Imperial.HandledCuffer;
using Content.Shared.Cuffs;
using Content.Shared.Cuffs.Components;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Components;
using Content.Shared.Bed.Sleep;
using Content.Shared.Stunnable;

namespace Content.Server.Imperial.HandledCuffer;

public sealed class HandledCufferSystem : EntitySystem
{
    [Dependency] private readonly SharedCuffableSystem _cuffs = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<HandledCufferComponent, AfterInteractEvent>(OnInteract);
    }
    private void OnInteract(EntityUid uid, HandledCufferComponent component, AfterInteractEvent args)
    {
        if (args.Target is not { Valid: true } target)
            return;

        if(!HasComp<SleepingComponent>(target) && !HasComp<StunnedComponent>(target))
            return;

        if (!args.CanReach)
            return;

        if(!TryComp<CuffableComponent>(args.Target, out var cuffableComp))
            return;

        if(!cuffableComp.CanStillInteract)
            return;

        var cuffs = EntityManager.SpawnEntity(component.SpawnedPrototype, Transform(uid).Coordinates);

        if(!TryComp<HandcuffComponent>(cuffs, out var cuffsComp))
            return;

        _cuffs.TryAddNewCuffs(target, args.User, cuffs, cuffableComp);
        args.Handled = true;
    }
}

