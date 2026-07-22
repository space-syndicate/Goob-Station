using Content.Server.Imperial.Power.Components.EventComponents;
using Content.Shared.Imperial.Power.Events;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.Imperial.Power.EntitySystems.EventSystems;

public sealed class SupermatterNoneEventSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _gameTiming = null!;
    [Dependency] private readonly IRobustRandom _random = null!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SupermatterNoneEventComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<SupermatterNoneEventComponent, ComponentRemove>(OnRemove);
    }

    private void OnInit(Entity<SupermatterNoneEventComponent> ent, ref ComponentInit args)
    {
        ent.Comp.Duration ??= TimeSpan.FromSeconds(_random.Next(200, 300));
        ent.Comp.EndTime ??= _gameTiming.CurTime + ent.Comp.Duration;
        ent.Comp.EventAfterTime ??= TimeSpan.FromSeconds(_random.Next(600, 900));

        var msg = Loc.GetString(ent.Comp.EventDesc);
        var ev = new SupermatterSendRadioEvent(msg);
        RaiseLocalEvent(ent.Owner, ref ev);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var currentTime = _gameTiming.CurTime;
        var query = EntityQueryEnumerator<SupermatterNoneEventComponent>();

        while (query.MoveNext(out var uid, out var none))
        {
            if (currentTime < none.EndTime)
                continue;

            RemComp<SupermatterNoneEventComponent>(uid);
        }
    }

    private void OnRemove(Entity<SupermatterNoneEventComponent> ent, ref ComponentRemove args)
    {
        var nextEventTime = ent.Comp.EventAfterTime!.Value;
        var ev = new SupermatterEventFinishedEvent(nextEventTime);
        RaiseLocalEvent(ent, ref ev);
    }
}
