using Content.Shared.Actions;
using Content.Shared.Imperial.XxRaay.Components;
using Content.Shared.Imperial.XxRaay.Systems;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;

namespace Content.Server.Imperial.XxRaay.Systems;

public sealed class WormBloodDrinkSystem : SharedWormBloodDrinkSystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly WormBloodSystem _wormBlood = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<WormBloodDrinkerComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(Entity<WormBloodDrinkerComponent> ent, ref MapInitEvent args)
    {
        _actions.AddAction(ent, ref ent.Comp.DrinkActionEntity, ent.Comp.DrinkAction);
    }

    protected override void OnDrinkerShutdown(Entity<WormBloodDrinkerComponent> ent)
    {
        if (ent.Comp.DrinkActionEntity != null)
            _actions.RemoveAction(ent.Owner, ent.Comp.DrinkActionEntity);
    }

    protected override void OnBloodDrained(EntityUid worm, EntityUid target, WormBloodDrinkerComponent drinker, int gained)
    {
        _wormBlood.TryAddBlood(worm, gained);
        _audio.PlayPvs(drinker.DrinkSound, worm);
    }
}
