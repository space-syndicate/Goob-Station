using Content.Server.Imperial.Power.Components.EventComponents;
using Content.Server.Radiation.Systems;
using Content.Shared.Imperial.Power.Components;
using Content.Shared.Imperial.Power.Events;
using Content.Shared.Radiation.Components;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.Imperial.Power.EntitySystems.EventSystems;

public sealed class SupermatterRadiationEventSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _gameTiming = null!;
    [Dependency] private readonly IRobustRandom _random = null!;
    [Dependency] private readonly RadiationSystem _radiationSystem = null!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SupermatterRadiationEventComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<SupermatterRadiationEventComponent, ComponentRemove>(OnRemove);
    }

    private void OnInit(Entity<SupermatterRadiationEventComponent> ent, ref ComponentInit args)
    {
        ent.Comp.Duration ??= TimeSpan.FromSeconds(_random.Next(80, 120));
        ent.Comp.EndTime ??= _gameTiming.CurTime + ent.Comp.Duration;

        ent.Comp.Intensity ??= _random.NextFloat(6f, 12f);
        ent.Comp.EventAfterTime ??= TimeSpan.FromSeconds(_random.Next(180, 420));

        EnsureComp<RadiationSourceComponent>(ent.Owner);
        _radiationSystem.SetIntensity(ent.Owner, ent.Comp.Intensity!.Value);

        var msg = Loc.GetString(ent.Comp.EventDesc);
        var ev = new SupermatterSendRadioEvent(msg);
        RaiseLocalEvent(ent.Owner, ref ev);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var currentTime = _gameTiming.CurTime;
        var query = EntityQueryEnumerator<SupermatterRadiationEventComponent>();

        while (query.MoveNext(out var uid, out var radEvent))
        {
            if (currentTime < radEvent.EndTime)
            {
                if (TryComp<SupermatterIntegrityComponent>(uid, out var integrity))
                {
                    if (!TryComp<RadiationSourceComponent>(uid, out var radSource))
                        continue;

                    _radiationSystem.SetIntensity((uid, radSource),
                        !integrity.Activated ? radEvent.DefaultIntensity : radEvent.Intensity!.Value);
                }
                continue;
            }

            RemComp<SupermatterRadiationEventComponent>(uid);
        }
    }

    private void OnRemove(Entity<SupermatterRadiationEventComponent> ent, ref ComponentRemove args)
    {
        if (TryComp<RadiationSourceComponent>(ent, out var radSource))
            _radiationSystem.SetIntensity((ent, radSource), ent.Comp.DefaultIntensity);

        var nextEventInterval = ent.Comp.EventAfterTime!.Value;
        var ev = new SupermatterEventFinishedEvent(nextEventInterval);
        RaiseLocalEvent(ent, ref ev);
    }
}
