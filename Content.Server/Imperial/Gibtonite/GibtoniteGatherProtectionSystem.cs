using Content.Server.Gatherable.Components;
using Content.Shared.Destructible;
using Content.Shared.Trigger.Components;
using Content.Shared.Trigger.Components.Effects;

namespace Content.Server.Imperial.Gibtonite;

/// <summary>
/// Imperial added Gatherable to AsteroidRock for plasma cutters. Gibtonite inherits it and
/// would be one-shot deleted on hit; cancel that path so the fuse can prime from damage.
/// Explosions still delete via QueueDel, not DestroyEntity.
/// </summary>
public sealed class GibtoniteGatherProtectionSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GatherableComponent, MapInitEvent>(OnGatherableMapInit);
        SubscribeLocalEvent<PreventInstantGatherComponent, DestructionAttemptEvent>(OnDestructionAttempt);
    }

    private void OnGatherableMapInit(Entity<GatherableComponent> ent, ref MapInitEvent args)
    {
        if (!HasComp<TimerTriggerComponent>(ent) || !HasComp<ExplodeOnTriggerComponent>(ent))
            return;

        EnsureComp<PreventInstantGatherComponent>(ent);
    }

    private void OnDestructionAttempt(Entity<PreventInstantGatherComponent> ent, ref DestructionAttemptEvent args)
    {
        args.Cancel();
    }
}
