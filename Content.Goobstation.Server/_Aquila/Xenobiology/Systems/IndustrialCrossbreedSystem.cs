using Content.Goobstation.Server._Aquila.Xenobiology.Components;
using Content.Shared.Examine;
using Content.Shared.Popups;
using Robust.Shared.Timing;

namespace Content.Goobstation.Server._Aquila.Xenobiology.Systems;

public sealed class IndustrialCrossbreedSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<IndustrialCrossbreedComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<IndustrialCrossbreedComponent, ExaminedEvent>(OnExamined);
    }

    private void OnMapInit(Entity<IndustrialCrossbreedComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.NextSpawn = _timing.CurTime + ent.Comp.Interval;
    }

    private void OnExamined(Entity<IndustrialCrossbreedComponent> ent, ref ExaminedEvent args)
    {
        args.PushMarkup(Loc.GetString("slime-crossbreed-industrial-cycles", ("cycles", ent.Comp.Cycles)));
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<IndustrialCrossbreedComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (_timing.CurTime < comp.NextSpawn)
                continue;

            comp.NextSpawn = _timing.CurTime + comp.Interval;
            foreach (var spawn in comp.Spawns)
            {
                SpawnNextToOrDrop(spawn, uid);
            }

            comp.Cycles--;
            if (comp.Cycles > 0)
                continue;

            _popup.PopupEntity(Loc.GetString("slime-crossbreed-industrial-depleted", ("item", uid)), uid);
            QueueDel(uid);
        }
    }
}
