using Content.Server.Atmos.EntitySystems;
using Content.Server.Imperial.Power.Components.EventComponents;
using Content.Shared.Atmos;
using Content.Shared.Imperial.Power.Components;
using Content.Shared.Imperial.Power.Events;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.Imperial.Power.EntitySystems.EventSystems;

public sealed class SupermatterPlasmaEventSystem : EntitySystem
{
    [Dependency] private readonly AtmosphereSystem _atmos = null!;
    [Dependency] private readonly IGameTiming _gameTiming = null!;
    [Dependency] private readonly IRobustRandom _random = null!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SupermatterPlasmaEventComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<SupermatterPlasmaEventComponent, ComponentRemove>(OnRemove);
    }

    private void OnInit(Entity<SupermatterPlasmaEventComponent> ent, ref ComponentInit args)
    {
        ent.Comp.HotspotTemperature ??= _random.NextFloat(1250f, 1500f);
        ent.Comp.HotspotVolume ??= _random.NextFloat(35f, 50f);
        ent.Comp.MolesAmount ??= _random.NextFloat(3f, 7f);

        var currentTime = _gameTiming.CurTime;
        ent.Comp.Duration ??= TimeSpan.FromSeconds(_random.Next(80, 120));
        ent.Comp.EndTime ??= currentTime + ent.Comp.Duration;
        ent.Comp.EventAfterTime ??= TimeSpan.FromSeconds(_random.Next(140, 180));

        ent.Comp.PlasmaInterval ??= TimeSpan.FromSeconds(_random.Next(6, 10));
        ent.Comp.NextPlasmaTime = currentTime + ent.Comp.PlasmaInterval.Value;

        var msg = Loc.GetString(ent.Comp.EventDesc);
        var ev = new SupermatterSendRadioEvent(msg);
        RaiseLocalEvent(ent.Owner, ref ev);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var currentTime = _gameTiming.CurTime;
        var query = EntityQueryEnumerator<SupermatterPlasmaEventComponent>();

        while (query.MoveNext(out var uid, out var plasma))
        {
            if (currentTime >= plasma.EndTime)
            {
                RemComp<SupermatterPlasmaEventComponent>(uid);
                continue;
            }

            if (TryComp<SupermatterIntegrityComponent>(uid, out var integrity) && !integrity.Activated)
                continue;

            if (currentTime < plasma.NextPlasmaTime)
                continue;
            plasma.NextPlasmaTime = currentTime + plasma.PlasmaInterval!.Value;

            var transComp = Transform(uid);
            var mix = _atmos.GetContainingMixture((uid, transComp));

            mix?.AdjustMoles(Gas.Plasma, plasma.MolesAmount!.Value);
            mix?.Temperature = MathF.Max(mix.Temperature, plasma.HotspotTemperature!.Value);
        }
    }

    private void OnRemove(Entity<SupermatterPlasmaEventComponent> ent, ref ComponentRemove args)
    {
        var nextEventInterval = ent.Comp.EventAfterTime!.Value;
        var ev = new SupermatterEventFinishedEvent(nextEventInterval);
        RaiseLocalEvent(ent, ref ev);
    }
}
