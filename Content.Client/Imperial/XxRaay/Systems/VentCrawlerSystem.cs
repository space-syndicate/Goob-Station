using Content.Client.Imperial.XxRaay.Components;
using Content.Client.SubFloor;
using Content.Shared.Imperial.XxRaay.Components;
using Content.Shared.Imperial.XxRaay.Systems;
using Content.Shared.SubFloor;
using Robust.Client.Player;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Client.Imperial.XxRaay.Systems;

public sealed class VentCrawlerSystem : SharedVentCrawlerSystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private readonly HashSet<Entity<SubFloorHideComponent>> _inRange = new();

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<LocalPlayerDetachedEvent>(OnPlayerDetached);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!_timing.IsFirstTimePredicted)
            return;

        var player = _player.LocalEntity;

        if (player == null ||
            !HasComp<ActiveVentCrawlingComponent>(player) ||
            !TryComp(player, out VentCrawlerComponent? crawler) ||
            !crawler.RevealPipeNetwork ||
            crawler.PipeRevealRange <= 0f)
        {
            ClearRevealed();
            return;
        }

        if (!TryComp(player, out TransformComponent? playerXform))
            return;

        _inRange.Clear();
        var playerPos = _transform.GetWorldPosition(playerXform);
        _lookup.GetEntitiesInRange(playerXform.MapID, playerPos, crawler.PipeRevealRange, _inRange, flags: TrayScannerSystem.Flags);

        var current = new HashSet<EntityUid>();

        foreach (var (uid, _) in _inRange)
        {
            current.Add(uid);
            EnsureComp<VentCrawlerRevealedComponent>(uid);
            SetRevealed(uid, true);
        }

        var revealedQuery = AllEntityQuery<VentCrawlerRevealedComponent>();
        while (revealedQuery.MoveNext(out var uid, out _))
        {
            if (current.Contains(uid))
                continue;

            SetRevealed(uid, false);
            RemCompDeferred<VentCrawlerRevealedComponent>(uid);
        }
    }

    private void OnPlayerDetached(LocalPlayerDetachedEvent ev)
    {
        ClearRevealed();
    }

    private void ClearRevealed()
    {
        var query = AllEntityQuery<VentCrawlerRevealedComponent>();
        while (query.MoveNext(out var uid, out _))
        {
            SetRevealed(uid, false);
            RemCompDeferred<VentCrawlerRevealedComponent>(uid);
        }
    }

    private void SetRevealed(EntityUid uid, bool value)
    {
        _appearance.SetData(uid, SubFloorVisuals.ScannerRevealed, value);
    }
}
