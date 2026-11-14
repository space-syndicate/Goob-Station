using System.Numerics;
using System.Threading.Tasks;
using Content.Server._BlackM.Elevator;
using Content.Server.Cargo.Systems;
using Content.Server.Chat.Systems;
using Content.Server.Station.Systems;
using Content.Shared.Cargo.Components;
using Content.Shared._BlackM.Underground;
using Content.Shared._BlackM.Elevator;
using Content.Shared.Chat;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs;
using Content.Shared.Interaction;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Popups;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._BlackM.Underground;

public sealed class UndergroundExpeditionSystem : EntitySystem
{
    private const int MaxGenerationAttempts = 5;
    private static readonly TimeSpan BoardingDuration = TimeSpan.FromSeconds(35);
    private static readonly TimeSpan FaultRecoveryDelay = TimeSpan.FromSeconds(30);
    private static readonly string[] AmsRewardParts =
    [
        "AmsPartCore", "AmsPartCoil", "AmsPartLens", "AmsPartCell", "AmsPartStabilizer", "AmsPartCasing",
    ];
    private static readonly TimeSpan AnnouncementDelay = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan EarlyReturnDelay = TimeSpan.FromSeconds(60);
    private static readonly SoundPathSpecifier OpenSound = new("/Audio/_BlackM/Interface/Terminal/term_open.ogg");
    private static readonly SoundPathSpecifier SelectSound = new("/Audio/_BlackM/Interface/Terminal/term_select.ogg");
    private static readonly SoundPathSpecifier ConfirmSound = new("/Audio/_BlackM/Interface/Terminal/term_confirm.ogg");
    private static readonly SoundPathSpecifier RewardSound = new("/Audio/Machines/high_tech_confirm.ogg");
    private static readonly SoundPathSpecifier ErrorSound = new("/Audio/_BlackM/Interface/Terminal/term_error.ogg");
    private static readonly SoundPathSpecifier LocatorSound = new("/Audio/Items/locator_beep.ogg");

    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly UndergroundMapSystem _sites = default!;
    [Dependency] private readonly UndergroundCabinSystem _cabins = default!;
    [Dependency] private readonly UndergroundCargoSystem _cargo = default!;
    [Dependency] private readonly BlackMElevatorSystem _elevators = default!;
    [Dependency] private readonly SharedMapSystem _maps = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedTransformSystem _transforms = default!;
    [Dependency] private readonly CargoSystem _cargoBank = default!;
    [Dependency] private readonly StationSystem _station = default!;
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<UndergroundConsoleComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<UndergroundConsoleComponent, InteractUsingEvent>(OnSampleUsed);
        SubscribeLocalEvent<UndergroundMissionTargetComponent, MobStateChangedEvent>(OnMissionTargetStateChanged);
        SubscribeLocalEvent<UndergroundRideOwnerComponent, BlackMElevatorCargoTripFinishedEvent>(OnRideFinished);
        Subs.BuiEvents<UndergroundTabletComponent>(UndergroundTabletUiKey.Key, subs =>
        {
            subs.Event<BoundUIOpenedEvent>(OnTabletOpened);
            subs.Event<UndergroundTabletEarlyReturnMessage>(OnTabletEarlyReturn);
        });
        Subs.BuiEvents<UndergroundConsoleComponent>(UndergroundConsoleUiKey.Key, subs =>
        {
            subs.Event<BoundUIOpenedEvent>(OnUiOpened);
            subs.Event<UndergroundConsoleStartMessage>(OnStart);
            subs.Event<UndergroundMissionSelectMessage>(OnMissionSelected);
            subs.Event<UndergroundMissionClaimMessage>(OnMissionClaim);
        });
    }

    private void OnStartup(Entity<UndergroundConsoleComponent> ent, ref ComponentStartup args)
    {
        if (!ent.Comp.IsReturn)
            EnsureComp<UndergroundRunComponent>(ent.Owner);
        UpdateConsole(ent.Owner);
    }

    private void OnUiOpened(Entity<UndergroundConsoleComponent> ent, ref BoundUIOpenedEvent args)
    {
        _audio.PlayPvs(OpenSound, ent.Owner);
        UpdateConsole(ent.Owner);
    }

    private void OnTabletOpened(Entity<UndergroundTabletComponent> ent, ref BoundUIOpenedEvent args)
    {
        _audio.PlayPvs(OpenSound, ent.Owner);
        UpdateTablet(ent.Owner, ent.Comp);
    }

    private void OnTabletEarlyReturn(Entity<UndergroundTabletComponent> ent,
        ref UndergroundTabletEarlyReturnMessage args)
    {
        if (!TryComp<HandsComponent>(args.Actor, out var hands) ||
            !_hands.IsHolding((args.Actor, hands), ent.Owner) ||
            ent.Comp.Console is not { } owner || TerminatingOrDeleted(owner) ||
            !TryComp<UndergroundRunComponent>(owner, out var run) ||
            run.Session.GenerationId != ent.Comp.Generation || run.Site is not { } site ||
            Transform(args.Actor).MapUid != site.Map ||
            !run.Session.TryRequestEarlyReturn(_timing.CurTime, EarlyReturnDelay))
            return;

        _audio.PlayPvs(ConfirmSound, ent.Owner);
        Warn(run, "underground-expedition-early-departure", (int) EarlyReturnDelay.TotalSeconds);
        Announce(run.LandingDispatcher, "underground-dispatcher-return-boarding");
        UpdateRun(owner, run);
    }

    private void OnStart(Entity<UndergroundConsoleComponent> ent, ref UndergroundConsoleStartMessage args)
    {
        if (ent.Comp.IsReturn || !TryComp<UndergroundRunComponent>(ent.Owner, out var run))
            return;
        if (!IsNearConsole(args.Actor, ent.Owner))
        {
            Error(ent.Owner, args.Actor, "underground-console-error-distance");
            return;
        }

        if (run.Session.Phase == UndergroundExpeditionPhase.Idle)
        {
            if (run.MissionRewardAvailable)
            {
                Error(ent.Owner, args.Actor, "underground-mission-claim-first");
                return;
            }
            if (!FindBoarding(ent.Owner, ent.Comp.LinkId, out var marker, out var width))
            {
                Error(ent.Owner, args.Actor, "underground-console-error-boarding");
                return;
            }
            if (!TryFindBoardingDispatcher(marker, out _))
            {
                Error(ent.Owner, args.Actor, "underground-console-error-dispatcher");
                return;
            }
            if (!run.Session.TryBeginGeneration(out var generation))
                return;
            _audio.PlayPvs(ConfirmSound, ent.Owner);
            run.MissionProgress = 0;
            run.MissionClaimed = false;
            run.MissionReturned = false;
            run.MissionSampleAnnounced = false;
            run.Boarding = marker;
            run.HalfWidth = width;
            UpdateRun(ent.Owner, run);
            _ = PrepareAsync(ent.Owner, run, generation, args.Actor);
            return;
        }

        if (run.Session.Phase != UndergroundExpeditionPhase.Ready || TerminatingOrDeleted(run.Boarding))
        {
            Error(ent.Owner, args.Actor, "underground-console-error-boarding");
            return;
        }

        if (run.OutboundAt != null)
            return;

        if (!TryFindBoardingDispatcher(run.Boarding, out var dispatcher))
        {
            Error(ent.Owner, args.Actor, "underground-console-error-dispatcher");
            return;
        }

        _audio.PlayPvs(ConfirmSound, ent.Owner);
        run.BoardingDispatcher = dispatcher;
        var announcementAt = _timing.CurTime + AnnouncementDelay;
        run.OutboundAnnouncementAt = announcementAt;
        run.OutboundAt = announcementAt + BoardingDuration;
        UpdateRun(ent.Owner, run);
        StationAnnounce(ent.Owner, Loc.GetString("underground-station-elevator-boarding",
            ("seconds", (int) BoardingDuration.TotalSeconds)));
    }

    private void OnMissionSelected(Entity<UndergroundConsoleComponent> ent, ref UndergroundMissionSelectMessage args)
    {
        if (ent.Comp.IsReturn || !TryComp<UndergroundRunComponent>(ent.Owner, out var run) ||
            run.Session.Phase != UndergroundExpeditionPhase.Idle || run.MissionRewardAvailable ||
            !UndergroundMissions.IsValid(args.Mission) || !IsNearConsole(args.Actor, ent.Owner))
            return;
        _audio.PlayPvs(SelectSound, ent.Owner);
        run.Mission = args.Mission;
        run.MissionProgress = 0;
        UpdateRun(ent.Owner, run);
    }

    private void OnMissionTargetStateChanged(Entity<UndergroundMissionTargetComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState != MobState.Dead || ent.Comp.Counted ||
            !TryComp<UndergroundRunComponent>(ent.Comp.Console, out var run) ||
            run.Session.GenerationId != ent.Comp.Generation || run.Session.Phase != UndergroundExpeditionPhase.Exploring ||
            run.Site is not { } site || Transform(ent.Owner).MapUid != site.Map)
            return;
        ent.Comp.Counted = true;
        if (UndergroundMissions.IsSample(run.Mission))
        {
            var capsule = Spawn(UndergroundMissions.Sample(run.Mission), Transform(ent.Owner).Coordinates);
            var marker = EnsureComp<UndergroundMissionSampleComponent>(capsule);
            marker.Console = ent.Comp.Console;
            marker.Generation = ent.Comp.Generation;
            marker.Mission = run.Mission;
            if (!run.MissionSampleAnnounced)
            {
                run.MissionSampleAnnounced = true;
                Warn(run, "underground-mission-sample-found");
            }
        }
        else
        {
            run.MissionProgress = Math.Min(run.MissionProgress + 1, UndergroundMissions.Goal(run.Mission));
            if (run.MissionProgress >= UndergroundMissions.Goal(run.Mission) && !run.MissionRewardAvailable)
            {
                run.MissionRewardAvailable = true;
                Warn(run, "underground-mission-hunt-complete");
            }
            UpdateRun(ent.Comp.Console, run);
        }
    }

    private void OnSampleUsed(Entity<UndergroundConsoleComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled || ent.Comp.IsReturn || !TryComp<UndergroundRunComponent>(ent.Owner, out var run) ||
            !UndergroundMissions.IsDelivery(run.Mission) || run.MissionRewardAvailable || run.MissionClaimed || !run.MissionReturned ||
            run.Session.Phase != UndergroundExpeditionPhase.Idle || !IsNearConsole(args.User, ent.Owner) ||
            !TryComp<UndergroundMissionSampleComponent>(args.Used, out var sample) ||
            sample.Console != ent.Owner || sample.Generation != run.Session.GenerationId || sample.Mission != run.Mission)
            return;
        args.Handled = true;
        _audio.PlayPvs(ConfirmSound, ent.Owner);
        run.MissionProgress = 1;
        run.MissionRewardAvailable = true;
        QueueDel(args.Used);
        UpdateRun(ent.Owner, run);
        _popup.PopupEntity(Loc.GetString(UndergroundMissions.IsItem(run.Mission) ? "underground-mission-item-accepted" : "underground-mission-sample-accepted"), ent.Owner, args.User);
    }

    private void OnMissionClaim(Entity<UndergroundConsoleComponent> ent, ref UndergroundMissionClaimMessage args)
    {
        if (ent.Comp.IsReturn || !TryComp<UndergroundRunComponent>(ent.Owner, out var run) ||
            run.Session.Phase != UndergroundExpeditionPhase.Idle || !run.MissionRewardAvailable ||
            !IsNearConsole(args.Actor, ent.Owner))
            return;
        run.MissionRewardAvailable = false;
        run.MissionClaimed = true;
        run.MissionProgress = 0;
        _audio.PlayPvs(RewardSound, ent.Owner);
        Spawn(UndergroundMissions.Reward(run.Mission), Transform(args.Actor).Coordinates);
        UpdateRun(ent.Owner, run);
    }

    private async Task PrepareAsync(EntityUid owner, UndergroundRunComponent run, long generation, EntityUid actor)
    {
        UndergroundGeneratedMap? site = null;
        UndergroundCabin? cabin = null;
        try
        {
            for (var attempt = 0; attempt < MaxGenerationAttempts; attempt++)
            {
                if (TerminatingOrDeleted(owner) || TerminatingOrDeleted(run.Boarding) ||
                    run.Session.Phase != UndergroundExpeditionPhase.Generating || run.Session.GenerationId != generation)
                    return;

                var result = await _sites.GenerateAsync(owner, _random.Next(int.MaxValue));
                site = result.Site;
                if (site != null || !result.Retryable)
                    break;
            }

            if (site == null || TerminatingOrDeleted(owner) ||
                run.Session.Phase != UndergroundExpeditionPhase.Generating || run.Session.GenerationId != generation ||
                TerminatingOrDeleted(run.Boarding))
                return;

            if (!_sites.TryPlaceMissionTargets(site, owner, run.Mission, generation))
            {
                Log.Error("Underground generation could not place mission targets.");
                return;
            }

            if (!_cabins.TryLoadCabin(out cabin) || cabin == null)
            {
                Log.Error("Underground preparation could not load its elevator cabin.");
                return;
            }

            if (!TryComp<MapGridComponent>(site.Map, out var grid) ||
                Transform(run.Boarding).MapUid == site.Map || Transform(run.Boarding).MapUid == cabin.Map ||
                site.Map == cabin.Map)
            {
                Log.Error("Underground preparation rejected invalid or overlapping map references.");
                return;
            }

            var coords = _maps.GridTileToLocal(site.Map, grid, site.LandingTile);
            var landing = Spawn("BlackMUndergroundLandingPoint", coords);
            EnsureComp<UndergroundRideOwnerComponent>(cabin.Controller).Console = owner;
            var fixtures = new HashSet<EntityUid>();
            var children = Transform(cabin.Grid).ChildEnumerator;
            while (children.MoveNext(out var child))
                fixtures.Add(child);

            run.Site = site;
            run.Cabin = cabin;
            run.Landing = landing;
            run.LandingDispatcher = site.LandingDispatcher;
            run.CabinFixtures = fixtures;
            if (!run.Session.GenerationFinished(generation, true))
                return;
            site = null;
            cabin = null;
            UpdateRun(owner, run);
            StationAnnounce(owner, Loc.GetString("underground-station-elevator-ready")); // BlackM
        }
        catch (Exception e)
        {
            Log.Error($"Underground preparation failed: {e}");
        }
        finally
        {
            if (site != null && !TerminatingOrDeleted(site.Map))
                QueueDel(site.Map);
            if (cabin != null && !TerminatingOrDeleted(cabin.Map))
                QueueDel(cabin.Map);
            if (!TerminatingOrDeleted(owner) && run.Session.Phase == UndergroundExpeditionPhase.Generating)
            {
                if (run.Session.GenerationFinished(generation, false))
                {
                    UpdateRun(owner, run);
                    Error(owner, actor, "underground-console-error-generation");
                }
            }
        }
    }

    private void StartRide(EntityUid owner, UndergroundRunComponent run, bool returning)
    {
        try
        {
            StartRideCore(owner, run, returning);
        }
        catch (Exception e)
        {
            Log.Error($"Underground ride could not start: {e}");
            Fail(owner, run);
        }
    }

    private void StartRideCore(EntityUid owner, UndergroundRunComponent run, bool returning)
    {
        if (run.Site is not { } site || run.Cabin is not { } cabin ||
            TerminatingOrDeleted(site.Map) || TerminatingOrDeleted(cabin.Map) ||
            TerminatingOrDeleted(run.Boarding) || TerminatingOrDeleted(run.Landing))
        {
            Fail(owner, run);
            return;
        }

        // A newly arrived elevator may still be cooling down. Never move cargo first.
        if (!TryComp<BlackMElevatorComponent>(cabin.Controller, out var elevator))
        {
            Fail(owner, run);
            return;
        }
        if (elevator.State != BlackMElevatorState.Idle)
            return;

        var from = returning ? run.Landing : run.Boarding;
        var to = returning ? run.Boarding : run.Landing;
        if (!_cargo.TryCollect(from, run.HalfWidth, out var cargo))
        {
            Fail(owner, run);
            return;
        }

        if (!returning)
        {
            var hasPlayer = false;
            foreach (var entity in cargo)
            {
                if (HasComp<ActorComponent>(entity))
                {
                    hasPlayer = true;
                    break;
                }
            }
            if (!hasPlayer)
            {
                Announce(run.BoardingDispatcher, "underground-dispatcher-outbound-cancelled");
                UpdateRun(owner, run);
                return;
            }
        }

        if (returning ? !run.Session.TryBeginReturn(_timing.CurTime) : !run.Session.TryBeginOutbound())
            return;

        elevator.Announcements.Clear();
        elevator.Announcements.Add(new BlackMElevatorAnnouncement { At = 2f, Message = returning
            ? "underground-dispatcher-ride-up-start" : "underground-dispatcher-ride-down-start" });
        elevator.Announcements.Add(new BlackMElevatorAnnouncement { At = 15f, Message = returning
            ? "underground-dispatcher-ride-up-middle" : "underground-dispatcher-ride-down-middle" });
        elevator.Announcements.Add(new BlackMElevatorAnnouncement { At = 25.5f, Message = returning
            ? "underground-dispatcher-ride-up-arrival" : "underground-dispatcher-ride-down-arrival" });

        Announce(returning ? run.LandingDispatcher : run.BoardingDispatcher,
            returning ? "underground-dispatcher-return-departing" : "underground-dispatcher-outbound-departing");
        var move = _cargo.MoveSnapshot(from, cabin.Origin, cargo, run.HalfWidth);
        if (move != UndergroundCargoMoveResult.Moved)
        {
            Fail(owner, run);
            return;
        }

        if (!_elevators.TryStartCargoTrip(cabin.Controller, cabin.Origin, to, cargo))
        {
            // Even if rollback succeeds, keep both maps until an administrator checks the ride.
            _cargo.MoveSnapshot(cabin.Origin, from, cargo, run.HalfWidth);
            Fail(owner, run);
            return;
        }

        UpdateRun(owner, run);
    }

    private void OnRideFinished(Entity<UndergroundRideOwnerComponent> ent,
        ref BlackMElevatorCargoTripFinishedEvent args)
    {
        var owner = ent.Comp.Console;
        if (!TryComp<UndergroundRunComponent>(owner, out var run))
            return;
        try
        {
            FinishRide(owner, run, ent.Owner, args.Succeeded);
        }
        catch (Exception e)
        {
            Log.Error($"Underground ride completion failed: {e}");
            Fail(owner, run);
        }
    }

    private void FinishRide(EntityUid owner, UndergroundRunComponent run, EntityUid controller, bool succeeded)
    {
        if (run.Cabin is not { } cabin || run.Site is not { } site || cabin.Controller != controller)
            return;

        var returning = run.Session.Phase == UndergroundExpeditionPhase.Returning;
        if (!returning && run.Session.Phase != UndergroundExpeditionPhase.Outbound)
            return;

        var destination = returning ? run.Boarding : run.Landing;
        if (!succeeded || TerminatingOrDeleted(destination) ||
            !_cargo.TryCollect(cabin.Origin, run.HalfWidth, out var leftovers) ||
            _cargo.MoveSnapshot(cabin.Origin, destination, leftovers, run.HalfWidth) != UndergroundCargoMoveResult.Moved ||
            HasUnexpectedCabinContents(run, cabin))
        {
            Fail(owner, run);
            return;
        }

        if (!returning)
        {
            run.Session.OutboundFinished(true, _timing.CurTime, TimeSpan.FromSeconds(UndergroundMissions.DurationSeconds(run.Mission)));
            // Remove tablets left from earlier generations before issuing the new one.
            RemoveTablets(owner);
            // Issue one tablet at the landing point for this expedition only.
            var tablet = Spawn("BlackMUndergroundContractTablet", Transform(run.Landing).Coordinates);
            var tabletComp = EnsureComp<UndergroundTabletComponent>(tablet);
            tabletComp.Console = owner;
            tabletComp.Generation = run.Session.GenerationId;
            UpdateRun(owner, run);
            return;
        }

        if (Transform(run.Boarding).MapUid == site.Map || Transform(run.Boarding).MapUid == cabin.Map ||
            site.Map == cabin.Map || TerminatingOrDeleted(site.Map) || TerminatingOrDeleted(cabin.Map))
        {
            Fail(owner, run);
            return;
        }

        var returnedGeneration = run.Session.GenerationId;
        var boardingMap = Transform(run.Boarding).MapUid;
        run.Session.ReturnFinished(true);
        RemoveTablets(owner);
        UpdateRun(owner, run);
        QueueDel(site.Map);
        QueueDel(cabin.Map);
        run.Site = null;
        run.Cabin = null;
        run.CabinFixtures.Clear();
        run.OutboundAt = null;
        run.OutboundAnnouncementAt = null;
        run.LandingDispatcher = null;
        run.DepartureAnnounced = false;
        run.Session.CleanupFinished();
        run.MissionReturned = true;
        AutoTurnIn(owner, run, returnedGeneration, boardingMap);
        UpdateRun(owner, run);
    }

    private void AutoTurnIn(EntityUid owner, UndergroundRunComponent run, long generation, EntityUid? boardingMap)
    {
        try
        {
            if (run.MissionClaimed)
                return;

            if (UndergroundMissions.IsDelivery(run.Mission) && !run.MissionRewardAvailable)
            {
                var delivered = new List<EntityUid>();
                var samples = EntityQueryEnumerator<UndergroundMissionSampleComponent, TransformComponent>();
                while (samples.MoveNext(out var uid, out var sample, out var xform))
                {
                    if (TerminatingOrDeleted(uid) || sample.Console != owner || sample.Generation != generation ||
                        sample.Mission != run.Mission || boardingMap == null || xform.MapUid != boardingMap)
                        continue;
                    delivered.Add(uid);
                }

                if (delivered.Count == 0)
                    return;

                foreach (var uid in delivered)
                    QueueDel(uid);
                run.MissionProgress = 1;
                run.MissionRewardAvailable = true;
            }

            if (!run.MissionRewardAvailable)
                return;

            var amount = UndergroundMissions.RewardAmount(run.Mission);
            if (_station.GetOwningStation(owner) is not { } station ||
                !TryComp<StationBankAccountComponent>(station, out var bank))
            {
                // No station account: keep the manual cash claim at the console as a fallback.
                return;
            }

            _cargoBank.UpdateBankAccount((station, bank), amount, bank.PrimaryAccount);
            run.MissionRewardAvailable = false;
            run.MissionClaimed = true;
            run.MissionProgress = 0;
            _audio.PlayPvs(RewardSound, owner);
            StationAnnounce(owner, Loc.GetString("underground-station-mission-paid", ("amount", amount)));

            GiveAmsReward(owner, run);
        }
        catch (Exception e)
        {
            Log.Error($"Underground auto turn-in failed: {e}");
        }
    }

    private void GiveAmsReward(EntityUid owner, UndergroundRunComponent run)
    {
        if (run.NextAmsRewardAt <= 0)
            run.NextAmsRewardAt = _random.Next(2, 4);

        run.CompletedExpeditions++;
        if (run.CompletedExpeditions < run.NextAmsRewardAt)
            return;

        run.CompletedExpeditions = 0;
        run.NextAmsRewardAt = _random.Next(2, 4);
        var coords = !TerminatingOrDeleted(run.Boarding) ? Transform(run.Boarding).Coordinates : Transform(owner).Coordinates;
        Spawn(_random.Pick(AmsRewardParts), coords);
        StationAnnounce(owner, Loc.GetString("underground-station-ams-reward"));
    }

    private void StationAnnounce(EntityUid source, string message)
    {
        if (TerminatingOrDeleted(source))
            return;
        _chat.DispatchStationAnnouncement(source, message, Loc.GetString("underground-dispatcher-sender"),
            colorOverride: Color.Gold);
    }

    private void TryRecoverFromFault(EntityUid owner, UndergroundRunComponent run)
    {
        try
        {
            if (run.Cabin is { } cabin && !TerminatingOrDeleted(cabin.Controller) &&
                TryComp<BlackMElevatorComponent>(cabin.Controller, out var elevator) &&
                elevator.State != BlackMElevatorState.Idle)
                return; // wait for the cabin to stop moving

            var mapsToDelete = new List<EntityUid>();
            if (run.Site is { } site && !TerminatingOrDeleted(site.Map))
                mapsToDelete.Add(site.Map);
            if (run.Cabin is { } c && !TerminatingOrDeleted(c.Map))
                mapsToDelete.Add(c.Map);

            if (mapsToDelete.Count > 0)
            {
                if (TerminatingOrDeleted(run.Boarding))
                    return;

                var safe = Transform(run.Boarding).Coordinates;
                var evacuate = new List<EntityUid>();
                var mobs = EntityQueryEnumerator<MobStateComponent, TransformComponent>();
                while (mobs.MoveNext(out var uid, out _, out var xform))
                {
                    if (xform.MapUid is { } map && mapsToDelete.Contains(map) && HasComp<ActorComponent>(uid))
                        evacuate.Add(uid);
                }
                foreach (var uid in evacuate)
                    _transforms.SetCoordinates(uid, safe);
                foreach (var map in mapsToDelete)
                    QueueDel(map);
            }

            RemoveTablets(owner);
            run.Site = null;
            run.Cabin = null;
            run.CabinFixtures.Clear();
            run.OutboundAt = null;
            run.OutboundAnnouncementAt = null;
            run.LandingDispatcher = null;
            run.DepartureAnnounced = false;
            run.FaultedAt = null;
            if (!run.Session.RecoverFromFault())
                return;
            Log.Warning($"Underground expedition {ToPrettyString(owner)} recovered from a fault.");
            StationAnnounce(owner, Loc.GetString("underground-station-recovered"));
            UpdateRun(owner, run);
        }
        catch (Exception e)
        {
            Log.Error($"Underground fault recovery failed: {e}");
            run.FaultedAt = _timing.CurTime;
        }
    }

    private bool HasUnexpectedCabinContents(UndergroundRunComponent run, UndergroundCabin cabin)
    {
        var children = Transform(cabin.Grid).ChildEnumerator;
        while (children.MoveNext(out var child))
        {
            if (!run.CabinFixtures.Contains(child) && !TerminatingOrDeleted(child))
                return true;
        }

        var actors = EntityQueryEnumerator<ActorComponent, TransformComponent>();
        while (actors.MoveNext(out _, out _, out var xform))
        {
            if (xform.MapUid == cabin.Map)
                return true;
        }
        return false;
    }

    private bool FindBoarding(EntityUid console, string linkId, out EntityUid marker, out float width)
    {
        marker = default;
        width = 0;
        var xform = Transform(console);
        if (xform.GridUid is not { } grid)
            return false;
        var consoles = EntityQueryEnumerator<UndergroundConsoleComponent, TransformComponent>();
        while (consoles.MoveNext(out var other, out var otherConsole, out var otherXform))
        {
            if (other != console && !otherConsole.IsReturn && otherConsole.LinkId == linkId &&
                otherXform.GridUid == grid)
                return false;
        }
        var found = false;
        var query = EntityQueryEnumerator<UndergroundBoardingZoneComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var zone, out var zoneXform))
        {
            if (zone.LinkId != linkId || zoneXform.GridUid != grid || zoneXform.ParentUid != grid)
                continue;
            if (found || zone.HalfWidth <= 0 || zone.HalfWidth > 1.5f)
                return false;
            found = true;
            marker = uid;
            width = zone.HalfWidth;
        }
        return found;
    }

    private bool IsNearConsole(EntityUid actor, EntityUid console)
    {
        if (TerminatingOrDeleted(actor) || TerminatingOrDeleted(console))
            return false;
        var a = Transform(actor);
        var b = Transform(console);
        return a.ParentUid == b.ParentUid && (a.LocalPosition - b.LocalPosition).LengthSquared() <= 6.25f;
    }

    // Popups and chat announcements are restricted to players on this expedition map.
    private void Warn(UndergroundRunComponent run, string key, int seconds = 0)
    {
        if (run.Site is not { } site)
            return;
        var message = Loc.GetString(key, ("seconds", seconds), ("minutes", seconds / 60));
        var filter = Filter.Empty();
        var recipients = 0;
        var query = EntityQueryEnumerator<ActorComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var actor, out var xform))
        {
            if (xform.MapUid != site.Map)
                continue;
            filter.AddPlayer(actor.PlayerSession);
            recipients++;
            _popup.PopupEntity(message, uid, uid, PopupType.LargeCaution);
        }
        if (recipients != 0)
        {
            // The fork's filtered announcement broadcasts sound globally, so keep this silent.
            _chat.DispatchFilteredAnnouncement(filter, message, source: run.LandingDispatcher,
                sender: Loc.GetString("underground-dispatcher-sender"), playSound: false);
        }
    }

    // Surface dispatchers are explicitly mapped; never create or silently choose
    // one if more than one is in range of this boarding marker.
    private bool TryFindBoardingDispatcher(EntityUid marker, out EntityUid result)
    {
        result = default;
        if (TerminatingOrDeleted(marker))
            return false;

        var origin = Transform(marker);
        var found = false;
        var query = EntityQueryEnumerator<BlackMElevatorDispatcherComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var dispatcher, out var xform))
        {
            if (TerminatingOrDeleted(uid) || dispatcher.ElevatorId != null ||
                xform.ParentUid != origin.ParentUid ||
                (xform.LocalPosition - origin.LocalPosition).LengthSquared() > 9f)
                continue;
            if (found)
                return false;
            result = uid;
            found = true;
        }
        return found;
    }

    private void Announce(EntityUid? dispatcher, string key)
    {
        if (dispatcher is not { } uid || TerminatingOrDeleted(uid))
            return;
        _chat.TrySendInGameICMessage(uid, Loc.GetString(key), InGameICChatType.Speak,
            hideChat: false, ignoreActionBlocker: true);
    }

    private void Error(EntityUid console, EntityUid actor, string key)
    {
        if (!TerminatingOrDeleted(console) && !TerminatingOrDeleted(actor))
        {
            _audio.PlayPvs(ErrorSound, console);
            _popup.PopupEntity(Loc.GetString(key), console, actor, PopupType.MediumCaution);
        }
    }

    private void RemoveTablets(EntityUid owner)
    {
        var tablets = EntityQueryEnumerator<UndergroundTabletComponent>();
        while (tablets.MoveNext(out var uid, out var tablet))
        {
            if (tablet.Console == owner && !TerminatingOrDeleted(uid))
                QueueDel(uid);
        }
    }

    // The locator follows only this expedition's loose delivery objective.
    // It runs only while the tablet is held and the expedition is exploring.
    private void UpdateLocators(TimeSpan now)
    {
        var tablets = EntityQueryEnumerator<UndergroundTabletComponent, TransformComponent>();
        while (tablets.MoveNext(out var uid, out var tablet, out var tabletTransform))
        {
            if (TerminatingOrDeleted(uid) || now < tablet.NextLocatorPing)
                continue;
            tablet.NextLocatorPing = now + TimeSpan.FromSeconds(1);
            var holder = tabletTransform.ParentUid;
            if (!TryComp<HandsComponent>(holder, out var hands) ||
                !_hands.IsHolding((holder, hands), uid) ||
                tablet.Console is not { } owner || TerminatingOrDeleted(owner) ||
                !TryComp<UndergroundRunComponent>(owner, out var run) ||
                run.Session.GenerationId != tablet.Generation ||
                run.Session.Phase != UndergroundExpeditionPhase.Exploring ||
                !UndergroundMissions.IsDelivery(run.Mission) || run.Site is not { } site ||
                tabletTransform.MapUid != site.Map)
                continue;

            var holderPosition = _transforms.GetWorldPosition(tabletTransform);
            var nearest = float.PositiveInfinity;
            var targets = EntityQueryEnumerator<UndergroundMissionSampleComponent, TransformComponent>();
            while (targets.MoveNext(out var target, out var sample, out var targetTransform))
            {
                // A recovered item in a hand/container is already found: stop pinging it.
                if (TerminatingOrDeleted(target) || sample.Console != owner ||
                    sample.Generation != tablet.Generation || sample.Mission != run.Mission ||
                    targetTransform.MapUid != site.Map || targetTransform.GridUid is not { } grid ||
                    targetTransform.ParentUid != grid)
                    continue;
                var distance = (_transforms.GetWorldPosition(targetTransform) - holderPosition).Length();
                nearest = Math.Min(nearest, distance);
            }
            if (float.IsPositiveInfinity(nearest))
                continue;

            tablet.NextLocatorPing = now + UndergroundLocator.GetInterval(nearest);
            _audio.PlayPvs(LocatorSound, holder, AudioParams.Default.WithVolume(-7f));
        }
    }

    private void Fail(EntityUid owner, UndergroundRunComponent run)
    {
        run.Session.Fail();
        run.FaultedAt = _timing.CurTime;
        Log.Error($"Underground expedition {ToPrettyString(owner)} faulted; maps retained for recovery.");
        UpdateRun(owner, run);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var now = _timing.CurTime;
        UpdateLocators(now);
        var query = EntityQueryEnumerator<UndergroundRunComponent>();
        while (query.MoveNext(out var owner, out var run))
        {
            if (run.Session.Phase == UndergroundExpeditionPhase.Ready && run.OutboundAt is { } outbound)
            {
                if (run.OutboundAnnouncementAt is { } announcement && now >= announcement)
                {
                    run.OutboundAnnouncementAt = null;
                    Announce(run.BoardingDispatcher, "underground-dispatcher-outbound-boarding");
                }
                if (now >= outbound)
                {
                    if (run.Cabin is { } readyCabin &&
                        TryComp<BlackMElevatorComponent>(readyCabin.Controller, out var readyElevator) &&
                        readyElevator.State != BlackMElevatorState.Idle)
                        continue;
                    run.OutboundAt = null;
                    StartRide(owner, run, false);
                    UpdateRun(owner, run);
                }
                else if (now >= run.NextUiUpdate)
                {
                    run.NextUiUpdate = now + TimeSpan.FromSeconds(1);
                    UpdateRun(owner, run);
                }
                continue;
            }
            if (run.Session.Phase == UndergroundExpeditionPhase.Faulted)
            {
                run.FaultedAt ??= now;
                if (now >= run.FaultedAt.Value + FaultRecoveryDelay)
                    TryRecoverFromFault(owner, run);
                continue;
            }
            if (run.Session.Phase != UndergroundExpeditionPhase.Exploring)
                continue;
            if (run.Session.ConsumeWarning(now) is { } seconds)
            {
                Warn(run, seconds >= 60 ? "underground-expedition-warning-minutes" : "underground-expedition-warning", seconds);
                if (seconds == 30)
                    Announce(run.LandingDispatcher, "underground-dispatcher-return-soon");
            }
            if (run.Session.TimeRemaining(now) <= TimeSpan.Zero)
            {
                if (!run.DepartureAnnounced)
                {
                    Warn(run, "underground-expedition-departing");
                    run.DepartureAnnounced = true;
                }
                StartRide(owner, run, true);
                continue;
            }
            if (now >= run.NextUiUpdate)
            {
                run.NextUiUpdate = now + TimeSpan.FromSeconds(1);
                UpdateRun(owner, run);
            }
        }
    }

    private void UpdateRun(EntityUid owner, UndergroundRunComponent run)
    {
        UpdateConsole(owner);
        var tablets = EntityQueryEnumerator<UndergroundTabletComponent>();
        while (tablets.MoveNext(out var uid, out var tablet))
        {
            if (tablet.Console == owner)
                UpdateTablet(uid, tablet);
        }
    }

    private void UpdateTablet(EntityUid tablet, UndergroundTabletComponent component)
    {
        if (TerminatingOrDeleted(tablet))
            return;
        if (component.Console is not { } owner || TerminatingOrDeleted(owner) ||
            !TryComp<UndergroundRunComponent>(owner, out var run) || run.Site == null ||
            run.Session.GenerationId != component.Generation)
        {
            _ui.SetUiState(tablet, UndergroundTabletUiKey.Key,
                new UndergroundConsoleState(UndergroundConsolePhase.Offline, 0, 0, false, false,
                    UndergroundMission.HeadcrabHunt, 0, false, false));
            return;
        }
        var phase = run.Session.Phase switch
        {
            UndergroundExpeditionPhase.Exploring => UndergroundConsolePhase.Exploring,
            UndergroundExpeditionPhase.Returning => UndergroundConsolePhase.Returning,
            UndergroundExpeditionPhase.AwaitingCleanup => UndergroundConsolePhase.Cleanup,
            UndergroundExpeditionPhase.Faulted => UndergroundConsolePhase.Faulted,
            _ => UndergroundConsolePhase.Offline,
        };
        var remaining = (int) Math.Ceiling(run.Session.TimeRemaining(_timing.CurTime).TotalSeconds);
        _ui.SetUiState(tablet, UndergroundTabletUiKey.Key,
            new UndergroundConsoleState(phase, remaining, UndergroundMissions.DurationSeconds(run.Mission),
                false, run.Session.CanRequestEarlyReturn(_timing.CurTime, EarlyReturnDelay),
                run.Mission, run.MissionProgress, false, false));
    }

    private void UpdateConsole(EntityUid console)
    {
        if (TerminatingOrDeleted(console) || !TryComp<UndergroundConsoleComponent>(console, out var comp))
            return;
        var owner = comp.IsReturn ? comp.MainConsole : console;
        if (owner is not { } main || !TryComp<UndergroundRunComponent>(main, out var run))
        {
            _ui.SetUiState(console, UndergroundConsoleUiKey.Key,
                new UndergroundConsoleState(UndergroundConsolePhase.Offline, 0, 600, false, false, UndergroundMission.HeadcrabHunt, 0, false, false));
            return;
        }
        var phase = run.Session.Phase switch
        {
            UndergroundExpeditionPhase.Idle => UndergroundConsolePhase.Idle,
            UndergroundExpeditionPhase.Generating => UndergroundConsolePhase.Generating,
            UndergroundExpeditionPhase.Ready => UndergroundConsolePhase.Ready,
            UndergroundExpeditionPhase.Outbound => UndergroundConsolePhase.Outbound,
            UndergroundExpeditionPhase.Exploring => UndergroundConsolePhase.Exploring,
            UndergroundExpeditionPhase.Returning => UndergroundConsolePhase.Returning,
            UndergroundExpeditionPhase.AwaitingCleanup => UndergroundConsolePhase.Cleanup,
            _ => UndergroundConsolePhase.Faulted,
        };
        if (phase == UndergroundConsolePhase.Ready && run.OutboundAt != null)
            phase = UndergroundConsolePhase.Boarding;
        var remaining = phase == UndergroundConsolePhase.Boarding && run.OutboundAt is { } outbound
            ? TimeSpan.FromTicks(Math.Max(0, (outbound - _timing.CurTime).Ticks))
            : run.Session.TimeRemaining(_timing.CurTime);
        var seconds = (int) Math.Ceiling(remaining.TotalSeconds);
        _ui.SetUiState(console, UndergroundConsoleUiKey.Key, new UndergroundConsoleState(
            phase, seconds, phase == UndergroundConsolePhase.Boarding ? (int) BoardingDuration.TotalSeconds
                : UndergroundMissions.DurationSeconds(run.Mission),
            !comp.IsReturn && (phase == UndergroundConsolePhase.Idle || phase == UndergroundConsolePhase.Ready),
            false, run.Mission, run.MissionProgress,
            !comp.IsReturn && phase == UndergroundConsolePhase.Idle && !run.MissionRewardAvailable,
            !comp.IsReturn && phase == UndergroundConsolePhase.Idle && run.MissionRewardAvailable));
    }
}
