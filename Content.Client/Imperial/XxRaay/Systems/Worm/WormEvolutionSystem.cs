using Content.Shared.Imperial.XxRaay.Components;
using Content.Shared.Imperial.XxRaay.Systems;

namespace Content.Client.Imperial.XxRaay.Systems;

public sealed class WormEvolutionSystem : SharedWormEvolutionSystem
{
    protected override void BeginEvolution(EntityUid worm, WormEvolverComponent evolver)
    {
        base.BeginEvolution(worm, evolver);
    }
}
