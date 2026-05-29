using Content.Shared.Imperial.XxRaay.Components;
using Content.Shared.Imperial.XxRaay.Systems;
using Robust.Shared.Audio.Systems;

namespace Content.Server.Imperial.XxRaay.Systems;

public sealed class WormBloodDrinkSystem : SharedWormBloodDrinkSystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly WormBloodSystem _wormBlood = default!;

    protected override void OnBloodDrained(EntityUid worm, EntityUid target, WormBloodDrinkerComponent drinker, int gained)
    {
        _wormBlood.TryAddBlood(worm, gained);
        _audio.PlayPvs(drinker.DrinkSound, worm);
    }
}
