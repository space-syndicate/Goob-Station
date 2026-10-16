using Content.Goobstation.Server._Aquila.Xenobiology.Components;
using Content.Shared.EntityEffects;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Timing;

namespace Content.Goobstation.Server._Aquila.Xenobiology.Systems;

public sealed class StabilizedCrossbreedSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedEntityEffectsSystem _effects = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<StabilizedCrossbreedComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (_timing.CurTime < comp.NextUpdate)
                continue;

            comp.NextUpdate = _timing.CurTime + comp.Interval;

            if (GetHolder((uid, comp)) is not { } holder || _mobState.IsDead(holder))
                continue;

            _effects.ApplyEffects(holder, comp.Effects);
        }
    }

    private EntityUid? GetHolder(Entity<StabilizedCrossbreedComponent> ent)
    {
        var current = ent.Owner;
        for (var i = 0; i < ent.Comp.MaxHolderDepth; i++)
        {
            if (!_container.TryGetContainingContainer((current, null, null), out var container))
                return null;

            current = container.Owner;
            if (HasComp<MobStateComponent>(current))
                return current;
        }

        return null;
    }
}
