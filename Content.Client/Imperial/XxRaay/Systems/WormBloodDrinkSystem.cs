using Content.Shared.Imperial.XxRaay.Components;
using Content.Shared.Imperial.XxRaay.Systems;

namespace Content.Client.Imperial.XxRaay.Systems;

public sealed class WormBloodDrinkSystem : SharedWormBloodDrinkSystem
{
    protected override void DrainBlood(EntityUid worm, EntityUid target, WormBloodDrinkerComponent drinker, int amount)
    {
    }
}
