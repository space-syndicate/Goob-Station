using Content.Shared.Imperial.XxRaay.Components;
using Content.Shared.Imperial.XxRaay.Systems;
using Content.Shared.FixedPoint;

namespace Content.Client.Imperial.XxRaay.Systems;

public sealed class WormBloodDrinkSystem : SharedWormBloodDrinkSystem
{
    protected override void DrainBlood(EntityUid worm, EntityUid target, WormBloodDrinkerComponent drinker, FixedPoint2 amount)
    {
        base.DrainBlood(worm, target, drinker, amount);
    }
}
