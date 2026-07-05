using Content.Shared.Imperial.XxRaay.Components;
using Content.Shared.Imperial.XxRaay.Systems;

namespace Content.Client.Imperial.XxRaay.Systems;

public sealed class WormReproductionSystem : SharedWormReproductionSystem
{
    protected override void BeginReproduction(EntityUid worm, WormReproducerComponent reproducer)
    {
        base.BeginReproduction(worm, reproducer);
    }
}
