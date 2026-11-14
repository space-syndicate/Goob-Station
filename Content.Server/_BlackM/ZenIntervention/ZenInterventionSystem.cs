using System.Linq;
using System.Numerics;
using Content.Server.Chat.Systems;
using Content.Server.GameTicking;
using Content.Server.Station.Components;
using Content.Server.Station.Systems;
using Content.Shared._BlackM.ZenIntervention;
using Content.Shared.GameTicking;
using Content.Shared.Station.Components;
using Robust.Server.GameStates;
using Robust.Shared.Localization;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._BlackM.ZenIntervention;

public sealed class ZenInterventionSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly ChatSystem _chatSystem = default!;
    [Dependency] private readonly PvsOverrideSystem _pvsOverride = default!;
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;

    private const float SyncInterval = 1f;

    private EntityUid? _singleton;
    private bool _roundWasActive;
    private float _syncAccumulator;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);
    }

    private void OnRoundRestart(RoundRestartCleanupEvent ev)
    {
        _singleton = null;
        _roundWasActive = false;
        _syncAccumulator = 0f;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_gameTicker.RunLevel != GameRunLevel.InRound)
        {
            _roundWasActive = false;
            return;
        }

        if (!_roundWasActive)
        {
            _roundWasActive = true;
            ResetState();
        }

        EnsureSingleton();

        if (_singleton is not { } uid || !TryComp<ZenInterventionComponent>(uid, out var comp))
            return;

        if (!comp.Enabled)
            return;

        Grow(uid, comp, frameTime);
        CheckBreach(comp);
        CheckWave(comp);
    }

    private void Grow(EntityUid uid, ZenInterventionComponent comp, float frameTime)
    {
        if (comp.Level >= comp.MaxLevel)
            return;

        var gainPerSecond = comp.GainPerMinute / 60f;
        var newLevel = comp.Level + gainPerSecond * frameTime;

        comp.Level = newLevel >= comp.MaxLevel ? comp.MaxLevel : newLevel;

        _syncAccumulator += frameTime;
        if (_syncAccumulator >= SyncInterval || comp.Level >= comp.MaxLevel)
        {
            _syncAccumulator = 0f;
            Dirty(uid, comp);
        }
    }

    private void CheckBreach(ZenInterventionComponent comp)
    {
        foreach (var threshold in comp.IntermediateBreachThresholds)
        {
            if (threshold <= 0f || threshold >= comp.BreachThreshold)
                continue;

            if (comp.Level < threshold)
            {
                comp.TriggeredIntermediateBreaches.Remove(threshold);
                continue;
            }

            if (comp.TriggeredIntermediateBreaches.Add(threshold))
                TriggerBreach(comp, comp.IntermediateBreachSpawnAmount, intermediate: true);
        }

        if (comp.Level < comp.BreachThreshold)
        {
            if (comp.BreachTriggered)
            {
                comp.BreachTriggered = false;
                Dirty(_singleton!.Value, comp);
            }

            return;
        }

        if (comp.BreachTriggered)
            return;

        comp.BreachTriggered = true;
        Dirty(_singleton!.Value, comp);

        TriggerBreach(comp);
    }

    private void CheckWave(ZenInterventionComponent comp)
    {
        if (comp.Level < comp.MaxLevel)
        {
            if (comp.WaveActive)
            {
                comp.WaveActive = false;
                Dirty(_singleton!.Value, comp);
            }

            return;
        }

        var curTime = _timing.CurTime;

        if (!comp.WaveActive)
        {
            comp.WaveActive = true;
            comp.NextWaveTime = curTime + comp.WaveInterval;
            Dirty(_singleton!.Value, comp);
            return;
        }

        if (curTime < comp.NextWaveTime)
            return;

        comp.NextWaveTime = curTime + comp.WaveInterval;
        Dirty(_singleton!.Value, comp);

        SpawnWave(comp);
    }

    public void TriggerBreach(ZenInterventionComponent comp, int? spawnAmount = null, bool intermediate = false)
    {
        if (!TryGetTargetStation(comp, out var stationUid))
            return;

        for (var i = 0; i < (spawnAmount ?? comp.BreachSpawnAmount); i++)
        {
            if (comp.BreachMobPrototypes.Count == 0)
                break;

            if (!TryGetRandomStationCoordinates(stationUid, comp, out var coords))
                continue;

            var protoId = _random.Pick(comp.BreachMobPrototypes);
            Spawn(protoId, coords);
        }

        _chatSystem.DispatchStationAnnouncement(
            stationUid,
            Loc.GetString(intermediate ? "zen-intervention-announce-minor-breach" : "zen-intervention-announce-breach"),
            Loc.GetString("zen-intervention-announcer-sender"),
            playDefaultSound: !intermediate,
            announcementSound: comp.BreachSound,
            colorOverride: comp.AnnouncementColor);
    }

    public void SpawnWave(ZenInterventionComponent comp)
    {
        if (!TryGetTargetStation(comp, out var stationUid))
            return;

        for (var i = 0; i < comp.WaveSpawnAmount; i++)
        {
            if (!TryGetRandomStationCoordinates(stationUid, comp, out var coords))
                continue;

            Spawn(comp.WaveMobPrototype, coords);
        }

        _chatSystem.DispatchStationAnnouncement(
            stationUid,
            Loc.GetString("zen-intervention-announce-wave"),
            Loc.GetString("zen-intervention-announcer-sender"),
            playDefaultSound: true,
            announcementSound: comp.WaveSound,
            colorOverride: comp.AnnouncementColor);
    }

    private void EnsureSingleton()
    {
        if (_singleton is { } uid && Exists(uid))
            return;

        _singleton = Spawn("ZenInterventionSingletonBlackM", MapCoordinates.Nullspace);

        _pvsOverride.AddGlobalOverride(_singleton.Value);
    }

    private bool TryGetTargetStation(ZenInterventionComponent comp, out EntityUid stationUid)
    {
        var query = EntityQueryEnumerator<StationDataComponent>();
        while (query.MoveNext(out var uid, out _))
        {
            var name = Name(uid);
            if (name.Contains(comp.TargetStationName, StringComparison.OrdinalIgnoreCase))
            {
                stationUid = uid;
                return true;
            }
        }

        stationUid = default;
        return false;
    }

    private bool TryGetRandomStationCoordinates(EntityUid stationUid, ZenInterventionComponent comp, out EntityCoordinates coords)
    {
        coords = default;

        if (!TryComp<StationDataComponent>(stationUid, out var stationData))
            return false;

        var grids = stationData.Grids.ToList();
        if (grids.Count == 0)
            return false;

        for (var attempt = 0; attempt < comp.MaxPlacementAttempts; attempt++)
        {
            var gridUid = _random.Pick(grids);

            if (!TryComp<MapGridComponent>(gridUid, out var grid))
                continue;

            var bounds = grid.LocalAABB;
            if (bounds.IsEmpty())
                continue;

            var randomLocal = new Vector2(
                _random.NextFloat(bounds.Left, bounds.Right),
                _random.NextFloat(bounds.Bottom, bounds.Top));

            var candidate = new EntityCoordinates(gridUid, randomLocal);

            var tileRef = _map.GetTileRef(gridUid, grid, candidate);
            if (tileRef.Tile.IsEmpty)
                continue;

            if (_lookup.AnyEntitiesIntersecting(candidate, LookupFlags.Static))
                continue;

            coords = candidate;
            return true;
        }

        return false;
    }

    public float GetLevel()
    {
        if (_singleton is { } uid && TryComp<ZenInterventionComponent>(uid, out var comp))
            return comp.Level;

        return 0f;
    }

    public void SetLevel(float value)
    {
        EnsureSingleton();

        if (_singleton is not { } uid || !TryComp<ZenInterventionComponent>(uid, out var comp))
            return;

        comp.Level = value < 0f ? 0f : value > comp.MaxLevel ? comp.MaxLevel : value;
        Dirty(uid, comp);
    }

    public void AdjustLevel(float delta)
    {
        EnsureSingleton();

        if (_singleton is not { } uid || !TryComp<ZenInterventionComponent>(uid, out var comp))
            return;

        SetLevel(comp.Level + delta);
    }

    public bool TryGetComponent(out ZenInterventionComponent comp)
    {
        EnsureSingleton();

        if (_singleton is { } uid && TryComp(uid, out comp!))
            return true;

        comp = default!;
        return false;
    }

    public void ForceTriggerBreach()
    {
        if (!TryGetComponent(out var comp))
            return;

        comp.BreachTriggered = true;
        Dirty(_singleton!.Value, comp);

        TriggerBreach(comp);
    }

    public void ForceSpawnWave()
    {
        if (!TryGetComponent(out var comp))
            return;

        SpawnWave(comp);
    }

    public void ResetState()
    {
        if (!TryGetComponent(out var comp))
            return;

        comp.Level = 0f;
        comp.TriggeredIntermediateBreaches.Clear();
        comp.BreachTriggered = false;
        comp.WaveActive = false;
        comp.NextWaveTime = TimeSpan.Zero;
        _syncAccumulator = 0f;
        Dirty(_singleton!.Value, comp);
    }
}
