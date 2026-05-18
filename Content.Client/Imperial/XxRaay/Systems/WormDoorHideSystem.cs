using Content.Client.Imperial.XxRaay.Components;
using Content.Shared.Eye;
using Content.Shared.Imperial.XxRaay.Components;
using Content.Shared.Imperial.XxRaay.Systems;
using Robust.Client.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Client.Imperial.XxRaay.Systems;

public sealed class WormDoorHideSystem : SharedWormDoorHideSystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly SharedEyeSystem _eye = default!;
    [Dependency] private readonly OccluderSystem _occluder = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<LocalPlayerDetachedEvent>(OnPlayerDetached);
    }

    protected override void OnActiveDoorHideStarted(EntityUid worm)
    {
        RefreshLocalEye(worm);
    }

    protected override void OnActiveDoorHideStopped(EntityUid worm)
    {
        RefreshLocalEye(worm);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!_timing.IsFirstTimePredicted)
            return;

        var player = _player.LocalEntity;

        if (player == null || !HasComp<ActiveWormDoorHidingComponent>(player))
        {
            ClearRevealed();
            return;
        }

        EntityUid? door = null;
        var occupiedQuery = AllEntityQuery<WormDoorHideOccupiedComponent>();

        while (occupiedQuery.MoveNext(out var uid, out var occupied))
        {
            if (occupied.Worm != player)
                continue;

            door = uid;
            break;
        }

        if (door == null)
        {
            ClearRevealed();
            return;
        }

        EnsureComp<WormDoorHideRevealedComponent>(door.Value);
        DisableLocalOccluder(door.Value);

        var revealedQuery = AllEntityQuery<WormDoorHideRevealedComponent>();
        while (revealedQuery.MoveNext(out var uid, out _))
        {
            if (uid == door)
                continue;

            RemCompDeferred<WormDoorHideRevealedComponent>(uid);
        }
    }

    private void RefreshLocalEye(EntityUid uid)
    {
        if (_player.LocalEntity != uid)
            return;

        if (TryComp(uid, out EyeComponent? eye))
            _eye.RefreshVisibilityMask((uid, eye));
    }

    private void OnPlayerDetached(LocalPlayerDetachedEvent ev)
    {
        ClearRevealed();
    }

    private void DisableLocalOccluder(EntityUid door)
    {
        if (!TryComp(door, out OccluderComponent? occluder) || !occluder.Enabled)
            return;

        _occluder.SetEnabled(door, false, occluder);
    }

    private void ClearRevealed()
    {
        var query = AllEntityQuery<WormDoorHideRevealedComponent>();
        while (query.MoveNext(out var uid, out _))
            RemCompDeferred<WormDoorHideRevealedComponent>(uid);
    }
}
