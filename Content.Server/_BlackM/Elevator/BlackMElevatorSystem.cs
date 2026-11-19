using System.Numerics;
using Content.Server.Chat.Systems;
using Content.Server.GameTicking;
using Content.Server.Spawners.EntitySystems;
using Content.Server.Station.Systems;
using Content.Shared._BlackM.Elevator;
using Content.Shared.Buckle;
using Content.Shared.Buckle.Components;
using Content.Shared.Chat;
using Content.Shared.Examine;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Tag;
using Content.Shared.Movement.Pulling.Systems;
using Robust.Shared.Audio.Systems;
using Robust.Shared.EntitySerialization;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._BlackM.Elevator;

public sealed class BlackMElevatorSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly MapLoaderSystem _mapLoader = default!;
    [Dependency] private readonly SharedTransformSystem _xform = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly ChatSystem _chatSystem = default!;
    [Dependency] private readonly SharedPointLightSystem _lights = default!;
    [Dependency] private readonly SharedBuckleSystem _buckle = default!;
    [Dependency] private readonly PullingSystem _pulling = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly TagSystem _tag = default!;
    [Dependency] private readonly StationSpawningSystem _stationSpawning = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PlayerSpawningEvent>(OnPlayerSpawning, before: new[] { typeof(SpawnPointSystem) });
        SubscribeLocalEvent<BlackMElevatorLightComponent, MapInitEvent>(OnLightMapInit);
        SubscribeLocalEvent<BlackMElevatorDispatcherComponent, ExaminedEvent>(OnDispatcherExamined);
    }

    private void OnPlayerSpawning(PlayerSpawningEvent ev)
    {
        if (ev.SpawnResult != null)
            return;

        if (ev.Station is not { } station)
            return;

        if (!TryComp(station, out BlackMElevatorStationComponent? cfg))
            return;

        if (ev.Job is not { } job || cfg.BlacklistedJobs.Contains(job))
            return;

        if (!EnsureElevator(station, cfg, out var elevator, out var comp))
            return;

        if (Transform(elevator).GridUid is not { } grid || !TryPickSpawn(grid, out var coords))
        {
            return;
        }

        var mob = _stationSpawning.SpawnPlayerMob(coords, job, ev.HumanoidCharacterProfile, station);
        ev.SpawnResult = mob;

        RegisterPassenger(elevator, comp, mob);
    }

    private bool EnsureElevator(EntityUid station, BlackMElevatorStationComponent cfg,
        out EntityUid elevator, out BlackMElevatorComponent comp)
    {
        elevator = default;
        comp = default!;

        if (cfg.Elevator == null)
        {
            if (cfg.LoadFailed)
                return false;

            var opts = new DeserializationOptions { InitializeMaps = true };
            if (!_mapLoader.TryLoadMap(cfg.ElevatorMap, out var map, out _, opts))
            {
                cfg.LoadFailed = true;
                return false;
            }

            var mapId = map.Value.Comp.MapId;
            cfg.ElevatorMapId = mapId;

            var q = EntityQueryEnumerator<BlackMElevatorComponent, TransformComponent>();
            while (q.MoveNext(out var uid, out var elev, out var xform))
            {
                if (xform.MapID != mapId || elev.Evacuation || elev.Id != cfg.SpawnElevatorId)
                    continue;
                cfg.Elevator = uid;
                break;
            }

            if (cfg.Elevator == null)
            {
                cfg.LoadFailed = true;
                return false;
            }
        }

        elevator = cfg.Elevator.Value;
        return TryComp(elevator, out comp!);
    }

    private bool TryPickSpawn(EntityUid grid, out EntityCoordinates coords, string? pointId = null)
    {
        var points = new List<EntityCoordinates>();
        var matched = new List<EntityCoordinates>();
        var q = EntityQueryEnumerator<BlackMElevatorSpawnPointComponent, TransformComponent>();
        while (q.MoveNext(out _, out var spawn, out var xform))
        {
            if (xform.GridUid != grid)
                continue;

            points.Add(xform.Coordinates);
            if (!string.IsNullOrEmpty(pointId) && spawn.PointId == pointId)
                matched.Add(xform.Coordinates);
        }

        if (matched.Count > 0)
            points = matched;

        coords = default;
        if (points.Count == 0)
            return false;

        _random.Shuffle(points);

        foreach (var point in points)
        {
            if (_lookup.GetEntitiesInRange<MobStateComponent>(point, 0.35f).Count == 0)
            {
                coords = point;
                return true;
            }
        }

        coords = points[0];
        return true;
    }

    private void RegisterPassenger(EntityUid uid, BlackMElevatorComponent comp, EntityUid mob)
    {
        var now = _timing.CurTime;

        switch (comp.State)
        {
            case BlackMElevatorState.Idle:
                comp.Passengers.Add(mob);
                comp.State = BlackMElevatorState.Boarding;
                comp.BoardingEnd = now + TimeSpan.FromSeconds(comp.BoardingDelay);
                break;

            case BlackMElevatorState.Boarding:
                comp.Passengers.Add(mob);
                break;

            case BlackMElevatorState.Descending:
                var remaining = comp.DescentTime - (float) (now - comp.StateStart).TotalSeconds;
                if (remaining > comp.JoinCutoff)
                    comp.Passengers.Add(mob);
                else
                    comp.Waiting.Add(mob);
                break;

            default:
                comp.Waiting.Add(mob);
                break;
        }
    }

    public override void Update(float frameTime)
    {
        var now = _timing.CurTime;
        var q = EntityQueryEnumerator<BlackMElevatorComponent>();

        while (q.MoveNext(out var uid, out var comp))
        {
            if (comp.Evacuation)
                UpdateEvacuation(uid, comp, now);

            switch (comp.State)
            {
                case BlackMElevatorState.Boarding:
                    if (!comp.Evacuation && now >= comp.BoardingEnd)
                        StartDescent(uid, comp);
                    break;

                case BlackMElevatorState.Descending:
                    UpdateDescent(uid, comp, now);
                    break;

                case BlackMElevatorState.Cooldown:
                    if (now >= comp.StateStart + TimeSpan.FromSeconds(comp.CooldownTime))
                        ResetElevator(uid, comp);
                    break;
            }

            if (comp.LightsRestoreAt is { } restore && now >= restore)
            {
                comp.LightsRestoreAt = null;
                ApplyLights(uid, comp.LightMode);
            }
        }
    }

    public bool TryStartCargoTrip(EntityUid elevator, EntityUid origin, EntityUid destination,
        IReadOnlyCollection<EntityUid> cargo)
    {
        if (TerminatingOrDeleted(elevator) || TerminatingOrDeleted(origin) || TerminatingOrDeleted(destination) ||
            !TryComp<BlackMElevatorComponent>(elevator, out var comp) || comp.Evacuation ||
            comp.State != BlackMElevatorState.Idle || comp.Passengers.Count != 0 || comp.Waiting.Count != 0 ||
            HasComp<BlackMElevatorCargoTripComponent>(elevator))
        {
            return false;
        }

        var originXform = Transform(origin);
        if (originXform.GridUid == null || originXform.ParentUid != originXform.GridUid ||
            Transform(elevator).GridUid != originXform.GridUid ||
            Transform(destination).GridUid == null ||
            Transform(destination).ParentUid != Transform(destination).GridUid)
        {
            return false;
        }

        var offsets = new Dictionary<EntityUid, Vector2>();
        foreach (var entity in cargo)
        {
            if (TerminatingOrDeleted(entity) || entity == elevator || entity == origin || entity == destination ||
                offsets.ContainsKey(entity))
            {
                return false;
            }

            var xform = Transform(entity);
            if (xform.ParentUid != originXform.ParentUid || xform.Anchored ||
                (TryComp<PhysicsComponent>(entity, out var physics) && physics.BodyType == BodyType.Static))
            {
                return false;
            }

            offsets.Add(entity, xform.LocalPosition - originXform.LocalPosition);
        }

        var trip = AddComp<BlackMElevatorCargoTripComponent>(elevator);
        trip.Target = destination;
        foreach (var (entity, offset) in offsets)
        {
            trip.Offsets.Add(entity, offset);
            comp.Passengers.Add(entity);
        }

        StartDescent(elevator, comp);
        return comp.State == BlackMElevatorState.Descending;
    }

    private void ArriveCargoTrip(EntityUid uid, BlackMElevatorComponent comp, BlackMElevatorCargoTripComponent trip)
    {
        CleanPassengers(comp);
        SendToPassengers(comp.Passengers, new BlackMElevatorRideEvent(false, 0f));
        var succeeded = !TerminatingOrDeleted(trip.Target);

        if (succeeded)
        {
            var targetXform = Transform(trip.Target);
            if (targetXform.GridUid == null || targetXform.ParentUid != targetXform.GridUid)
            {
                succeeded = false;
            }
            else
            {
                foreach (var (entity, offset) in trip.Offsets)
                {
                    if (TerminatingOrDeleted(entity))
                        continue;

                    // A caller must keep the cabin isolated while the trip is running.
                    var entityXform = Transform(entity);
                    if (entityXform.GridUid != Transform(uid).GridUid ||
                        entityXform.ParentUid != Transform(uid).GridUid || entityXform.Anchored)
                    {
                        succeeded = false;
                        continue;
                    }

                    try
                    {
                        ReleaseHolds(entity);
                        _xform.SetCoordinates(entity, new EntityCoordinates(targetXform.ParentUid, targetXform.LocalPosition + offset));
                    }
                    catch (Exception e)
                    {
                        Log.Error($"Could not transfer {ToPrettyString(entity)} in elevator {ToPrettyString(uid)}: {e}");
                        succeeded = false;
                    }
                }
            }
        }

        if (succeeded)
            Jolt(comp.Passengers, comp.LandingStrength);

        SendFade(comp.Passengers, false, comp.FadeInTime);
        comp.Passengers.Clear();
        SetLightMode(uid, comp, BlackMElevatorLightMode.Normal);
        comp.State = BlackMElevatorState.Cooldown;
        comp.StateStart = _timing.CurTime;
        RemComp<BlackMElevatorCargoTripComponent>(uid);
        var ev = new BlackMElevatorCargoTripFinishedEvent(succeeded);
        RaiseLocalEvent(uid, ref ev);
    }

    private void StartDescent(EntityUid uid, BlackMElevatorComponent comp)
    {
        CleanPassengers(comp);

        if (comp.Passengers.Count == 0 && !HasComp<BlackMElevatorCargoTripComponent>(uid))
        {
            comp.State = BlackMElevatorState.Idle;
            return;
        }

        var now = _timing.CurTime;
        comp.State = BlackMElevatorState.Descending;
        comp.StateStart = now;
        comp.Announcements.Sort((a, b) => a.At.CompareTo(b.At));
        comp.HeavyShakes.Sort();
        comp.AnnounceIndex = 0;
        comp.HeavyIndex = 0;
        comp.FadedOut = false;
        comp.Braked = false;
        comp.NextShake = now + TimeSpan.FromSeconds(_random.NextFloat(3f, 5f));
        comp.NextRideSync = now;

        SetLightMode(uid, comp, BlackMElevatorLightMode.Dim);

        if (comp.StartSound != null)
            _audio.PlayPvs(comp.StartSound, uid);

        Jolt(comp.Passengers, comp.HeavyShakeStrength);
        SyncRide(comp, now);

    }

    private void UpdateDescent(EntityUid uid, BlackMElevatorComponent comp, TimeSpan now)
    {
        var elapsed = (float) (now - comp.StateStart).TotalSeconds;

        while (comp.AnnounceIndex < comp.Announcements.Count
               && elapsed >= comp.Announcements[comp.AnnounceIndex].At)
        {
            var ann = comp.Announcements[comp.AnnounceIndex++];
            Notify(uid, comp, Loc.GetString(ann.Message), ann.Big);
        }

        while (comp.HeavyIndex < comp.HeavyShakes.Count && elapsed >= comp.HeavyShakes[comp.HeavyIndex])
        {
            comp.HeavyIndex++;
            Jolt(comp.Passengers, comp.HeavyShakeStrength);
            FlickerLights(uid, comp, 0.4f, 0.9f);
        }

        var brakeAt = comp.BrakeAt ?? comp.DescentTime - comp.FadeOutLead - 1.5f;
        if (!comp.Braked && elapsed >= brakeAt)
        {
            comp.Braked = true;
            Jolt(comp.Passengers, comp.BrakeStrength);
            comp.NextRideSync = now;
        }

        if (now >= comp.NextRideSync)
            SyncRide(comp, now);

        if (!comp.Braked && now >= comp.NextShake)
        {
            comp.NextShake = now + TimeSpan.FromSeconds(_random.NextFloat(3f, 6f));
            Jolt(comp.Passengers, _random.NextFloat(comp.SmallShakeMin, comp.SmallShakeMax));

            if (_random.Prob(0.25f))
                FlickerLights(uid, comp, 0.15f, 0.4f);
        }

        if (comp.LightMode != BlackMElevatorLightMode.Emergency && elapsed >= comp.EmergencyLightsAt)
            SetLightMode(uid, comp, BlackMElevatorLightMode.Emergency);

        if (!comp.FadedOut && elapsed >= comp.DescentTime - comp.FadeOutLead)
        {
            comp.FadedOut = true;
            SendFade(comp.Passengers, true, comp.FadeOutLead, Loc.GetString(comp.FadeText));
        }

        if (elapsed >= comp.DescentTime)
            Arrive(uid, comp);
    }

    private void Arrive(EntityUid uid, BlackMElevatorComponent comp)
    {
        if (TryComp<BlackMElevatorCargoTripComponent>(uid, out var cargoTrip))
        {
            ArriveCargoTrip(uid, comp, cargoTrip);
            return;
        }

        CleanPassengers(comp);
        SendToPassengers(comp.Passengers, new BlackMElevatorRideEvent(false, 0f));

        var dests = new List<EntityUid>();
        var q = EntityQueryEnumerator<BlackMElevatorDestinationComponent>();
        while (q.MoveNext(out var dUid, out var dest))
        {
            if (dest.Id == comp.DestinationId)
                dests.Add(dUid);
        }

        if (dests.Count == 0)
        {
            SendFade(comp.Passengers, false, comp.FadeInTime);
        }
        else
        {
            _random.Shuffle(dests);
            var i = 0;

            foreach (var p in comp.Passengers)
            {
                var dest = dests[i++ % dests.Count];
                ReleaseHolds(p);
                _xform.SetCoordinates(p, Transform(dest).Coordinates);
                RaiseLocalEvent(new BlackMElevatorArrivedEvent(p, comp.Id));
            }

            Jolt(comp.Passengers, comp.LandingStrength);

            SendFade(comp.Passengers, false, comp.FadeInTime);
            comp.Passengers.Clear();
        }

        SetLightMode(uid, comp, BlackMElevatorLightMode.Normal);

        comp.State = BlackMElevatorState.Cooldown;
        comp.StateStart = _timing.CurTime;
    }

    private void ResetElevator(EntityUid uid, BlackMElevatorComponent comp)
    {
        comp.Passengers.UnionWith(comp.Waiting);
        comp.Waiting.Clear();
        CleanPassengers(comp);

        SetLightMode(uid, comp, BlackMElevatorLightMode.Normal);

        if (comp.Passengers.Count > 0)
        {
            comp.State = BlackMElevatorState.Boarding;
            comp.BoardingEnd = _timing.CurTime + TimeSpan.FromSeconds(comp.BoardingDelay);
        }
        else
        {
            comp.State = BlackMElevatorState.Idle;
        }

    }

    public bool SetEvacuation(string elevatorId, bool enabled)
    {
        var found = false;
        var q = EntityQueryEnumerator<BlackMElevatorComponent>();
        while (q.MoveNext(out _, out var comp))
        {
            if (!comp.Evacuation || comp.Id != elevatorId)
                continue;

            comp.Enabled = enabled;
            found = true;
        }

        return found;
    }

    public bool TryStartRandomEvacuation(string elevatorId, out string location)
    {
        location = string.Empty;
        var candidates = new List<BlackMElevatorComponent>();

        var q = EntityQueryEnumerator<BlackMElevatorComponent>();
        while (q.MoveNext(out _, out var comp))
        {
            if (!comp.Evacuation || comp.Id != elevatorId)
                continue;

            if (comp.Enabled)
                return false;

            candidates.Add(comp);
        }

        if (candidates.Count == 0)
            return false;

        var chosen = candidates[_random.Next(candidates.Count)];
        chosen.Enabled = true;
        location = GetLocationName(chosen);
        return true;
    }

    public string GetLocationName(BlackMElevatorComponent comp)
    {
        if (!string.IsNullOrWhiteSpace(comp.LocationName))
            return Loc.TryGetString(comp.LocationName, out var loc) ? loc : comp.LocationName;

        if (!string.IsNullOrWhiteSpace(comp.PointId))
            return comp.PointId;

        return Loc.GetString("blackm-elevator-evac-location-unknown");
    }

    private static bool SamePoint(string? a, string? b)
    {
        return string.IsNullOrEmpty(a) ? string.IsNullOrEmpty(b) : a == b;
    }

    private const float ScanInterval = 0.5f;

    private void UpdateEvacuation(EntityUid uid, BlackMElevatorComponent comp, TimeSpan now)
    {
        HashSet<EntityUid>? occupants = null;
        if (now >= comp.NextScan)
        {
            comp.NextScan = now + TimeSpan.FromSeconds(ScanInterval);
            occupants = CollectEvacPassengers(comp);
        }

        switch (comp.State)
        {
            case BlackMElevatorState.Idle:
                if (!comp.Enabled)
                {
                    if (occupants != null)
                        PopupNewcomers(comp, occupants, Loc.GetString("blackm-elevator-popup-disabled"));
                    return;
                }

                OpenBoarding(comp);
                return;

            case BlackMElevatorState.Boarding:
                if (!comp.Enabled)
                {
                    StopCountdown(comp, null);
                    comp.State = BlackMElevatorState.Idle;
                    return;
                }

                UpdateBoarding(uid, comp, now, occupants);
                return;

            case BlackMElevatorState.Descending:
            case BlackMElevatorState.Cooldown:
                if (occupants != null)
                {
                    PopupNewcomers(comp, occupants, Loc.GetString("blackm-elevator-popup-away",
                        ("seconds", SecondsUntilReturn(comp, now))));
                }
                return;
        }
    }

    private void OpenBoarding(BlackMElevatorComponent comp)
    {
        comp.State = BlackMElevatorState.Boarding;
        comp.CountdownActive = false;
        comp.BoardingWarned = false;
        comp.LastCountdownSecond = int.MaxValue;
        comp.InZone.Clear();
        comp.Faded.Clear();

        var boarding = Loc.GetString(comp.BoardingMessage,
            ("seconds", (int) comp.BoardingDelay),
            ("location", GetLocationName(comp)));
        if (comp.GlobalAnnounce)
            AnnounceGlobal(Loc.GetString(comp.Sender), boarding, Color.Cyan);
        SayAtBoarding(comp, boarding);
    }

    private void UpdateBoarding(EntityUid uid, BlackMElevatorComponent comp, TimeSpan now, HashSet<EntityUid>? occupants)
    {
        if (!comp.CountdownActive)
        {
            if (occupants == null)
                return;

            if (occupants.Count == 0)
            {
                comp.InZone.Clear();
                return;
            }

            comp.CountdownActive = true;
            comp.BoardingEnd = now + TimeSpan.FromSeconds(comp.BoardingDelay);
            comp.BoardingWarned = comp.BoardingDelay <= comp.ClosingWarningAt + 1f;
            comp.LastCountdownSecond = int.MaxValue;
            SayAtBoarding(comp, Loc.GetString(comp.CountdownStartMessage, ("seconds", (int) comp.BoardingDelay)));
        }

        var remaining = (float) (comp.BoardingEnd - now).TotalSeconds;
        var seconds = (int) MathF.Ceiling(MathF.Max(remaining, 0f));

        if (occupants != null)
            PopupNewcomers(comp, occupants, Loc.GetString("blackm-elevator-popup-boarding", ("seconds", seconds)));

        if (!comp.BoardingWarned && remaining <= comp.ClosingWarningAt)
        {
            comp.BoardingWarned = true;
            SayAtBoarding(comp, Loc.GetString(comp.ClosingMessage, ("seconds", (int) comp.ClosingWarningAt)));
        }

        if (seconds > 0 && seconds <= comp.PopupCountdownFrom && seconds < comp.LastCountdownSecond)
        {
            comp.LastCountdownSecond = seconds;
            var text = Loc.GetString("blackm-elevator-popup-countdown", ("seconds", seconds));
            foreach (var p in comp.InZone)
            {
                _popup.PopupEntity(text, p, p, PopupType.Large);
            }
        }

        if (comp.DepartFadeTime > 0f && remaining <= comp.DepartFadeTime)
        {
            var fade = new List<EntityUid>();
            foreach (var p in comp.InZone)
            {
                if (comp.Faded.Add(p))
                    fade.Add(p);
            }

            if (fade.Count > 0)
                SendFade(fade, true, MathF.Max(remaining, 0.1f));
        }

        if (remaining <= 0f)
            BeginEvacDeparture(uid, comp);
    }

    private void BeginEvacDeparture(EntityUid uid, BlackMElevatorComponent comp)
    {
        var grid = Transform(uid).GridUid;
        var boarded = CollectEvacPassengers(comp);

        if (boarded.Count == 0 || grid == null)
        {
            StopCountdown(comp, null);
            SayAtBoarding(comp, Loc.GetString(comp.CancelMessage));
            return;
        }

        SayAtBoarding(comp, Loc.GetString(comp.DepartingMessage));

        foreach (var p in boarded)
        {
            ReleaseHolds(p);

            if (TryPickSpawn(grid.Value, out var coords, comp.PointId))
                _xform.SetCoordinates(p, coords);

            comp.Passengers.Add(p);
        }

        SendFade(boarded, false, comp.FadeInTime);
        StopCountdown(comp, boarded);
        StartDescent(uid, comp);
    }

    private void StopCountdown(BlackMElevatorComponent comp, HashSet<EntityUid>? boarded)
    {
        var restore = new List<EntityUid>();
        foreach (var p in comp.Faded)
        {
            if (boarded == null || !boarded.Contains(p))
                restore.Add(p);
        }

        if (restore.Count > 0)
            SendFade(restore, false, 0.5f);

        comp.Faded.Clear();
        comp.InZone.Clear();
        comp.CountdownActive = false;
        comp.BoardingWarned = false;
        comp.LastCountdownSecond = int.MaxValue;
    }

    private void PopupNewcomers(BlackMElevatorComponent comp, HashSet<EntityUid> occupants, string text)
    {
        foreach (var p in occupants)
        {
            if (!comp.InZone.Contains(p))
                _popup.PopupEntity(text, p, p, PopupType.Medium);
        }

        comp.InZone.Clear();
        comp.InZone.UnionWith(occupants);
    }

    private int SecondsUntilReturn(BlackMElevatorComponent comp, TimeSpan now)
    {
        var elapsed = (float) (now - comp.StateStart).TotalSeconds;
        var left = comp.State switch
        {
            BlackMElevatorState.Descending => comp.DescentTime - elapsed + comp.CooldownTime,
            BlackMElevatorState.Cooldown => comp.CooldownTime - elapsed,
            _ => 0f,
        };

        return (int) MathF.Ceiling(MathF.Max(left, 0f));
    }

    private void OnDispatcherExamined(Entity<BlackMElevatorDispatcherComponent> ent, ref ExaminedEvent args)
    {
        if (ent.Comp.ElevatorId is not { } id)
            return;

        var q = EntityQueryEnumerator<BlackMElevatorComponent>();
        while (q.MoveNext(out _, out var comp))
        {
            if (!comp.Evacuation || comp.Id != id || !SamePoint(comp.PointId, ent.Comp.PointId))
                continue;

            args.PushMarkup(StatusText(comp));
            return;
        }
    }

    private string StatusText(BlackMElevatorComponent comp)
    {
        var now = _timing.CurTime;

        switch (comp.State)
        {
            case BlackMElevatorState.Descending:
            case BlackMElevatorState.Cooldown:
                return Loc.GetString("blackm-elevator-examine-away", ("seconds", SecondsUntilReturn(comp, now)));
        }

        if (!comp.Enabled)
            return Loc.GetString("blackm-elevator-examine-disabled");

        if (comp.State == BlackMElevatorState.Boarding && comp.CountdownActive)
        {
            var seconds = (int) MathF.Ceiling(MathF.Max((float) (comp.BoardingEnd - now).TotalSeconds, 0f));
            return Loc.GetString("blackm-elevator-examine-boarding", ("seconds", seconds));
        }

        return Loc.GetString("blackm-elevator-examine-ready", ("seconds", (int) comp.BoardingDelay));
    }

    private HashSet<EntityUid> CollectEvacPassengers(BlackMElevatorComponent comp)
    {
        var result = new HashSet<EntityUid>();
        var q = EntityQueryEnumerator<BlackMElevatorBoardZoneComponent, TransformComponent>();

        while (q.MoveNext(out _, out var zone, out var xform))
        {
            if (zone.ElevatorId != comp.Id || !SamePoint(zone.PointId, comp.PointId))
                continue;

            foreach (var ent in _lookup.GetEntitiesInRange<MobStateComponent>(xform.Coordinates, zone.Range))
            {
                if (_mobState.IsDead(ent.Owner) || !HasComp<ActorComponent>(ent.Owner))
                    continue;

                if (zone.RestrictedTags.Count > 0 && _tag.HasAnyTag(ent.Owner, zone.RestrictedTags))
                    continue;

                result.Add(ent.Owner);
            }
        }

        return result;
    }

    private void AnnounceGlobal(string sender, string message, Color color)
    {
        _chatSystem.DispatchGlobalAnnouncement(
            message,
            sender: sender,
            playSound: true,
            colorOverride: color);
    }

    private void Jolt(IEnumerable<EntityUid> targets, float strength)
    {
        if (strength <= 0f)
            return;

        SendToPassengers(targets, new BlackMElevatorJoltEvent(strength));
    }

    private void SyncRide(BlackMElevatorComponent comp, TimeSpan now)
    {
        comp.NextRideSync = now + TimeSpan.FromSeconds(1);
        var intensity = comp.RumbleIntensity * (comp.Braked ? 0.4f : 1f);
        SendToPassengers(comp.Passengers, new BlackMElevatorRideEvent(intensity > 0f, intensity));
    }

    private void SendToPassengers(IEnumerable<EntityUid> targets, EntityEventArgs ev)
    {
        foreach (var t in targets)
        {
            if (TryComp(t, out ActorComponent? actor))
                RaiseNetworkEvent(ev, actor.PlayerSession);
        }
    }

    private void Notify(EntityUid elevator, BlackMElevatorComponent comp, string text, bool big)
    {
        if (!TryGetDispatcher(elevator, comp, out var dispatcher))
            return;

        if (big)
            text = text.ToUpperInvariant();

        _chatSystem.TrySendInGameICMessage(dispatcher, text, InGameICChatType.Speak,
            hideChat: false, ignoreActionBlocker: true);
    }

    private bool TryGetDispatcher(EntityUid elevator, BlackMElevatorComponent comp, out EntityUid dispatcher)
    {
        var grid = Transform(elevator).GridUid;
        EntityUid? fallback = null;
        var q = EntityQueryEnumerator<BlackMElevatorDispatcherComponent, TransformComponent>();
        while (q.MoveNext(out var uid, out var disp, out var xform))
        {
            if (xform.GridUid != grid || disp.ElevatorId != null)
                continue;

            if (SamePoint(disp.PointId, comp.PointId))
            {
                dispatcher = uid;
                return true;
            }

            fallback ??= uid;
        }

        dispatcher = fallback ?? default;
        return fallback != null;
    }

    private void SayAtBoarding(BlackMElevatorComponent comp, string text)
    {
        var q = EntityQueryEnumerator<BlackMElevatorDispatcherComponent>();
        while (q.MoveNext(out var uid, out var disp))
        {
            if (disp.ElevatorId != comp.Id || !SamePoint(disp.PointId, comp.PointId))
                continue;

            _chatSystem.TrySendInGameICMessage(uid, text, InGameICChatType.Speak,
                hideChat: false, ignoreActionBlocker: true);
        }
    }

    private void SendFade(IEnumerable<EntityUid> targets, bool toBlack, float duration, string? text = null)
    {
        var ev = new BlackMElevatorFadeEvent(toBlack, duration, text);
        foreach (var t in targets)
        {
            if (TryComp(t, out ActorComponent? actor))
                RaiseNetworkEvent(ev, actor.PlayerSession);
        }
    }

    private void OnLightMapInit(Entity<BlackMElevatorLightComponent> ent, ref MapInitEvent args)
    {
        if (!_lights.TryGetLight(ent, out var light))
            return;

        ent.Comp.BaseEnergy = light.Energy;
        ent.Comp.BaseColor = light.Color;
    }

    private void SetLightMode(EntityUid uid, BlackMElevatorComponent comp, BlackMElevatorLightMode mode)
    {
        comp.LightMode = mode;
        ApplyLights(uid, mode);
    }

    private void ApplyLights(EntityUid elevator, BlackMElevatorLightMode mode)
    {
        var grid = Transform(elevator).GridUid;
        var q = EntityQueryEnumerator<BlackMElevatorLightComponent, TransformComponent>();

        while (q.MoveNext(out var uid, out var light, out var xform))
        {
            if (xform.GridUid != grid)
                continue;

            _lights.SetEnabled(uid, true);

            switch (mode)
            {
                case BlackMElevatorLightMode.Normal:
                    _lights.SetEnergy(uid, light.BaseEnergy);
                    _lights.SetColor(uid, light.BaseColor);
                    break;
                case BlackMElevatorLightMode.Dim:
                    _lights.SetEnergy(uid, light.BaseEnergy * 0.45f);
                    _lights.SetColor(uid, light.BaseColor);
                    break;
                case BlackMElevatorLightMode.Emergency:
                    _lights.SetEnergy(uid, light.BaseEnergy * 0.8f);
                    _lights.SetColor(uid, Color.FromHex("#ff2a1a"));
                    break;
            }
        }
    }

    private void FlickerLights(EntityUid elevator, BlackMElevatorComponent comp, float min, float max)
    {
        var grid = Transform(elevator).GridUid;
        var q = EntityQueryEnumerator<BlackMElevatorLightComponent, TransformComponent>();

        while (q.MoveNext(out var uid, out _, out var xform))
        {
            if (xform.GridUid == grid)
                _lights.SetEnabled(uid, false);
        }

        comp.LightsRestoreAt = _timing.CurTime + TimeSpan.FromSeconds(_random.NextFloat(min, max));
    }

    private void ReleaseHolds(EntityUid p)
    {
        _pulling.StopAllPulls(p);

        if (TryComp(p, out BuckleComponent? buckle) && buckle.Buckled)
            _buckle.Unbuckle((p, buckle), null);
    }

    private void CleanPassengers(BlackMElevatorComponent comp)
    {
        comp.Passengers.RemoveWhere(p => TerminatingOrDeleted(p));
        comp.Waiting.RemoveWhere(p => TerminatingOrDeleted(p));
    }
}