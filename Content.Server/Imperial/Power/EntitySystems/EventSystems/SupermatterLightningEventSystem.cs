using Content.Server.Imperial.Power.Components.EventComponents;
using Content.Server.Lightning;
using Content.Shared.Imperial.Power.Components;
using Content.Shared.Imperial.Power.Events;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.Imperial.Power.EntitySystems.EventSystems;

public sealed class SupermatterLightningEventSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _gameTiming = null!;
    [Dependency] private readonly IRobustRandom _random = null!;
    [Dependency] private readonly LightningSystem _lightning = null!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SupermatterLightningEventComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<SupermatterLightningEventComponent, ComponentRemove>(OnRemove);
    }

    private void OnInit(Entity<SupermatterLightningEventComponent> ent, ref ComponentInit args)
    {
        ent.Comp.Duration ??= TimeSpan.FromSeconds(_random.Next(80, 120));
        ent.Comp.EndTime ??= _gameTiming.CurTime + ent.Comp.Duration;
        ent.Comp.EventAfterTime ??= TimeSpan.FromSeconds(_random.Next(140, 180));

        ent.Comp.LightningInterval ??= TimeSpan.FromSeconds(_random.Next(6, 10));

        var msg = Loc.GetString(ent.Comp.EventDesc);
        var ev = new SupermatterSendRadioEvent(msg);
        RaiseLocalEvent(ent.Owner, ref ev);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var currentTime = _gameTiming.CurTime;
        var query = EntityQueryEnumerator<SupermatterLightningEventComponent>();

        while (query.MoveNext(out var uid, out var lightning))
        {
            if (currentTime >= lightning.EndTime)
            {
                RemComp<SupermatterLightningEventComponent>(uid);
                continue;
            }

            if (TryComp<SupermatterIntegrityComponent>(uid, out var integrity) && !integrity.Activated)
                continue;

            if (currentTime < lightning.NextLightningTime)
                continue;

            lightning.NextLightningTime = currentTime + lightning.LightningInterval!.Value;
            _lightning.ShootRandomLightnings(uid, lightning.BoltRadius, lightning.BoltCount);
        }
    }

    private void OnRemove(Entity<SupermatterLightningEventComponent> ent, ref ComponentRemove args)
    {
        var nextEventInterval = ent.Comp.EventAfterTime!.Value;
        var ev = new SupermatterEventFinishedEvent(nextEventInterval);
        RaiseLocalEvent(ent, ref ev);
    }
}
