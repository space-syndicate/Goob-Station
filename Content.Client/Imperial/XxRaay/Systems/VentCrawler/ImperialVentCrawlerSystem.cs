using Content.Client.Imperial.XxRaay.Components;
using Content.Client.SubFloor;
using Content.Shared.Imperial.XxRaay.Components;
using Content.Shared.Imperial.XxRaay.Systems;
using Content.Shared.SubFloor;
using Robust.Client.Player;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Client.Imperial.XxRaay.Systems;

public sealed class ImperialVentCrawlerSystem : SharedImperialVentCrawlerSystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private static readonly TimeSpan PipeRevealRefresh = TimeSpan.FromMilliseconds(200);
    private readonly HashSet<Entity<SubFloorHideComponent>> _inRange = new();
    private readonly HashSet<EntityUid> _current = new();
    private TimeSpan _nextPipeRefresh;

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
        var active = player != null &&
            HasComp<ActiveVentCrawlingComponent>(player) &&
            TryComp(player, out ImperialVentCrawlerComponent? crawler) &&
            crawler.RevealPipeNetwork &&
            crawler.PipeRevealRange > 0f;

        if (_timing.CurTime >= _nextPipeRefresh)
        {
            _nextPipeRefresh = _timing.CurTime + PipeRevealRefresh;

            if (active && TryComp(player, out TransformComponent? playerXform))
            {
                var comp = Comp<ImperialVentCrawlerComponent>(player.Value);
                _inRange.Clear();
                var playerPos = _transform.GetWorldPosition(playerXform);
                _lookup.GetEntitiesInRange(playerXform.MapID, playerPos, comp.PipeRevealRange, _inRange, flags: TrayScannerSystem.Flags);

                _current.Clear();
                foreach (var (uid, _) in _inRange)
                {
                    _current.Add(uid);
                    EnsureComp<ImperialVentCrawlerRevealedComponent>(uid);
                }
            }
            else
            {
                _current.Clear();
            }
        }

        var revealedQuery = AllEntityQuery<ImperialVentCrawlerRevealedComponent>();
        while (revealedQuery.MoveNext(out var uid, out _))
        {
            if (active && _current.Contains(uid))
            {
                SetRevealed(uid, true);
            }
            else
            {
                SetRevealed(uid, false);
                RemCompDeferred<ImperialVentCrawlerRevealedComponent>(uid);
            }
        }
    }

    private void OnPlayerDetached(LocalPlayerDetachedEvent ev)
    {
        ClearRevealed();
    }

    private void ClearRevealed()
    {
        _current.Clear();
        var query = AllEntityQuery<ImperialVentCrawlerRevealedComponent>();
        while (query.MoveNext(out var uid, out _))
        {
            SetRevealed(uid, false);
            RemCompDeferred<ImperialVentCrawlerRevealedComponent>(uid);
        }
    }

    private void SetRevealed(EntityUid uid, bool value)
    {
        _appearance.SetData(uid, SubFloorVisuals.ScannerRevealed, value);
    }
}
