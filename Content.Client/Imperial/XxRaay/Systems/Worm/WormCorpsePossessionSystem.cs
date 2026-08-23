using Content.Shared.Imperial.XxRaay.Components;
using Content.Shared.Imperial.XxRaay.Systems;

namespace Content.Client.Imperial.XxRaay.Systems;

public sealed class WormCorpsePossessionSystem : SharedWormCorpsePossessionSystem
{
    protected override void TryPossess(EntityUid worm, EntityUid corpse, WormCorpseHostComponent host)
    {
    }

    protected override void ExitPossession(EntityUid worm, EntityUid corpse, bool forced)
    {
    }
}
