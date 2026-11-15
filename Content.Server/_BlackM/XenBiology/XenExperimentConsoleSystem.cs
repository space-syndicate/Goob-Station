using Content.Server.Atmos.EntitySystems;
using Content.Server.Chat.Systems;
using Content.Server.Power.EntitySystems;
using Content.Server.Research.Systems;
using Content.Server.Station.Systems;
using Content.Shared._BlackM.XenBiology;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Power;
using Content.Shared.Physics;
using Content.Shared.Research.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._BlackM.XenBiology;

public sealed class XenExperimentConsoleSystem : EntitySystem
{
    [Dependency] private readonly AppearanceSystem _appearance = default!;
    [Dependency] private readonly AtmosphereSystem _atmosphere = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly ItemSlotsSystem _itemSlots = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ResearchSystem _research = default!;
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly UserInterfaceSystem _userInterface = default!;

    private const float UiUpdateInterval = 0.5f;
    private const float MaximumValue = 100f;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<XenExperimentConsoleComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<XenExperimentConsoleComponent, EntInsertedIntoContainerMessage>(OnSampleInserted);
        SubscribeLocalEvent<XenExperimentConsoleComponent, EntRemovedFromContainerMessage>(OnSampleRemoved);
        SubscribeLocalEvent<XenExperimentConsoleComponent, PowerChangedEvent>(OnPowerChanged);
        SubscribeLocalEvent<XenExperimentConsoleComponent, ResearchServerPointsChangedEvent>(OnResearchPointsChanged);
        SubscribeLocalEvent<XenExperimentResultComponent, ExaminedEvent>(OnResultExamined);
        SubscribeLocalEvent<XenExperimentConsoleComponent, InteractUsingEvent>(
            OnInteractUsing,
            after: [typeof(ItemSlotsSystem)]);

        Subs.BuiEvents<XenExperimentConsoleComponent>(XenExperimentConsoleUiKey.Key,
            subs =>
            {
                subs.Event<BoundUIOpenedEvent>(OnUiOpened);
                subs.Event<XenExperimentStartMessage>(OnStartMessage);
            });
    }

    private void OnStartup(Entity<XenExperimentConsoleComponent> ent, ref ComponentStartup args)
    {
        UpdateUserInterface(ent);
    }

    private void OnSampleInserted(Entity<XenExperimentConsoleComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        if (args.Container.ID != ent.Comp.SampleSlotId &&
            args.Container.ID != ent.Comp.CapacitorSlotId)
            return;

        UpdateUserInterface(ent);
    }

    private void OnSampleRemoved(Entity<XenExperimentConsoleComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        if (args.Container.ID != ent.Comp.SampleSlotId &&
            args.Container.ID != ent.Comp.CapacitorSlotId)
            return;

        UpdateUserInterface(ent);
    }

    private void OnPowerChanged(Entity<XenExperimentConsoleComponent> ent, ref PowerChangedEvent args)
    {
        if (!args.Powered && ent.Comp.RunningExperiment != null)
            CancelExperiment(ent);

        UpdateUserInterface(ent);
    }

    private void OnResearchPointsChanged(Entity<XenExperimentConsoleComponent> ent,
        ref ResearchServerPointsChangedEvent args)
    {
        UpdateUserInterface(ent);
    }

    private void OnUiOpened(Entity<XenExperimentConsoleComponent> ent, ref BoundUIOpenedEvent args)
    {
        UpdateUserInterface(ent);
    }

    private void OnInteractUsing(Entity<XenExperimentConsoleComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled ||
            HasComp<ScientificCrusherInputComponent>(args.Used) ||
            HasComp<XenExperimentCapacitorComponent>(args.Used))
        {
            return;
        }

        args.Handled = _userInterface.TryOpenUi(ent.Owner, XenExperimentConsoleUiKey.Key, args.User);
    }

    private void OnResultExamined(Entity<XenExperimentResultComponent> ent, ref ExaminedEvent args)
    {
        var message = ent.Comp.Quality switch
        {
            XenExperimentResultQuality.Analyzed => "xen-experiment-result-examine-analyzed",
            XenExperimentResultQuality.Unstable => "xen-experiment-result-examine-unstable",
            XenExperimentResultQuality.Spoiled => "xen-experiment-result-examine-spoiled",
            _ => "xen-experiment-result-examine-spoiled"
        };

        args.PushMarkup(Loc.GetString(message, ("data", ent.Comp.RecordedData)));
    }

    private void OnStartMessage(Entity<XenExperimentConsoleComponent> ent, ref XenExperimentStartMessage args)
    {
        TryStartExperiment(ent, args.ExperimentId, args.Actor);
    }

    public bool TryStartExperiment(
        Entity<XenExperimentConsoleComponent> console,
        ProtoId<XenExperimentPrototype> experimentId,
        EntityUid user)
    {
        if (!CanStartExperiment(console, experimentId, user))
            return false;

        var experiment = _prototype.Index(experimentId);
        DoStartExperiment(console, experiment, user);
        return true;
    }

    public bool CanStartExperiment(
        Entity<XenExperimentConsoleComponent> console,
        ProtoId<XenExperimentPrototype> experimentId,
        EntityUid user,
        bool quiet = false)
    {
        if (!_prototype.TryIndex(experimentId, out var experiment))
        {
            ShowError(console, user, "xen-experiment-console-error-invalid-experiment", quiet);
            return false;
        }

        if (console.Comp.RunningExperiment != null)
        {
            ShowError(console, user, "xen-experiment-console-error-running", quiet);
            return false;
        }

        if (!this.IsPowered(console, EntityManager))
        {
            ShowError(console, user, "xen-experiment-console-error-no-power", quiet);
            return false;
        }

        if (!TryComp<ResearchClientComponent>(console, out var client) ||
            !_research.TryGetClientServer(console, out _, out _, client))
        {
            ShowError(console, user, "xen-experiment-console-error-no-server", quiet);
            return false;
        }

        if (console.Comp.Stability < experiment.MinimumStability)
        {
            ShowError(console, user, "xen-experiment-console-error-stability", quiet);
            return false;
        }

        if (!_itemSlots.TryGetSlot(console, console.Comp.SampleSlotId, out var slot) ||
            slot.Item is not { Valid: true } sample)
        {
            ShowError(console, user, "xen-experiment-console-error-no-sample", quiet);
            return false;
        }

        if (HasComp<XenExperimentResultComponent>(sample))
        {
            ShowError(console, user, "xen-experiment-console-error-processed-sample", quiet);
            return false;
        }

        var samplePrototype = MetaData(sample).EntityPrototype;
        if (samplePrototype == null || new EntProtoId(samplePrototype.ID) != experiment.RequiredSample)
        {
            ShowError(console, user, "xen-experiment-console-error-wrong-sample", quiet);
            return false;
        }

        return true;
    }

    private void DoStartExperiment(
        Entity<XenExperimentConsoleComponent> console,
        XenExperimentPrototype experiment,
        EntityUid user)
    {
        if (!_itemSlots.TryGetSlot(console, console.Comp.SampleSlotId, out var slot) ||
            slot.Item is not { Valid: true })
            return;

        _itemSlots.SetLock(console, slot, true);
        _itemSlots.SetLock(console, console.Comp.CapacitorSlotId, true);

        console.Comp.Stability = Math.Clamp(
            console.Comp.Stability - experiment.StabilityCost,
            0f,
            MaximumValue);
        console.Comp.RunningExperiment = experiment.ID;
        console.Comp.ExperimentEndTime = _timing.CurTime + experiment.Duration;
        var active = EnsureComp<ActiveXenExperimentConsoleComponent>(console);
        active.User = user; // BlackM personal objectives
        active.IncidentChance = Math.Clamp(
            experiment.IncidentChance * GetIncidentChanceMultiplier(console),
            0f,
            1f);

        _popup.PopupEntity(
            Loc.GetString("xen-experiment-console-started", ("experiment", Loc.GetString(experiment.Name))),
            console,
            user,
            PopupType.Medium);
        UpdateUserInterface(console);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<ActiveXenExperimentConsoleComponent, XenExperimentConsoleComponent>();
        while (query.MoveNext(out var uid, out var active, out var console))
        {
            if (console.RunningExperiment != null && console.ExperimentEndTime <= _timing.CurTime)
            {
                FinishExperiment((uid, console), active);
                if (Deleted(uid))
                    continue;
            }

            if (console.RunningExperiment == null)
            {
                console.Stability = Math.Min(
                    MaximumValue,
                    console.Stability + console.StabilityRecoveryPerSecond * frameTime);
            }

            active.UiUpdateAccumulator += frameTime;
            if (active.UiUpdateAccumulator >= UiUpdateInterval)
            {
                active.UiUpdateAccumulator -= UiUpdateInterval;
                UpdateUserInterface((uid, console));
            }

            if (console.RunningExperiment == null && console.Stability >= MaximumValue)
            {
                RemCompDeferred<ActiveXenExperimentConsoleComponent>(uid);
            }
        }
    }

    private void FinishExperiment(
        Entity<XenExperimentConsoleComponent> console,
        ActiveXenExperimentConsoleComponent active)
    {
        if (console.Comp.RunningExperiment is not { } experimentId ||
            !_prototype.TryIndex(experimentId, out var experiment))
        {
            console.Comp.RunningExperiment = null;
            UnlockExperimentSlots(console);
            UpdateUserInterface(console);
            return;
        }

        console.Comp.RunningExperiment = null;
        var incidentOccurred = _random.Prob(active.IncidentChance);
        if (incidentOccurred)
        {
            _chat.DispatchStationAnnouncement(
                console,
                Loc.GetString("xen-experiment-console-incident-announcement"),
                Loc.GetString("xen-experiment-console-incident-announcement-sender"));

            for (var i = 0; i < experiment.IncidentCount; i++)
                DoIncident(console, experiment.Level);

            BurnOutCapacitor(console);
        }
        else
        {
            _itemSlots.SetLock(console, console.Comp.CapacitorSlotId, false);
        }

        var quality = incidentOccurred
            ? XenExperimentResultQuality.Unstable
            : XenExperimentResultQuality.Analyzed;
        RecordResult(console, experiment, quality);
        // personal objectives
        if (TryComp<ActiveXenExperimentConsoleComponent>(console, out var blackmActive) && blackmActive.User is { } blackmUser)
        {
            var blackmObjectives = EntityManager.System<Content.Server._BlackM.PersonalObjectives.PersonalObjectivesSystem>();
            blackmObjectives.AddProgress(blackmUser, Content.Shared._BlackM.PersonalObjectives.BlackMPersonalCounters.XenExperiments);
            if (!incidentOccurred)
                blackmObjectives.AddProgress(blackmUser, Content.Shared._BlackM.PersonalObjectives.BlackMPersonalCounters.XenExperimentsClean);
        }

        var recordedData = GetRecordedData(experiment.Reward, quality);
        _popup.PopupEntity(
            Loc.GetString(
                "xen-experiment-console-completed",
                ("experiment", Loc.GetString(experiment.Name)),
                ("reward", recordedData)),
            console,
            PopupType.Large);

        if (!Deleted(console))
            UpdateUserInterface(console);
    }

    private void CancelExperiment(Entity<XenExperimentConsoleComponent> console)
    {
        if (console.Comp.RunningExperiment is { } experimentId &&
            _prototype.TryIndex(experimentId, out var experiment))
        {
            RecordResult(console, experiment, XenExperimentResultQuality.Spoiled);
        }
        else
        {
            _itemSlots.SetLock(console, console.Comp.SampleSlotId, false);
        }

        console.Comp.RunningExperiment = null;
        _itemSlots.SetLock(console, console.Comp.CapacitorSlotId, false);
        _popup.PopupEntity(
            Loc.GetString("xen-experiment-console-cancelled-power"),
            console,
            PopupType.LargeCaution);
    }

    private void DoIncident(
        Entity<XenExperimentConsoleComponent> console,
        XenExperimentLevel level)
    {
        switch (_random.Next(3))
        {
            case 0:
                SpawnStationIncident(console, console.Comp.ElectricalDischarge);
                ShowIncident(console, "xen-experiment-console-incident-sparks");
                break;
            case 1:
                SpawnStationIncident(console, console.Comp.RadiationEffect);
                ShowIncident(console, "xen-experiment-console-incident-radiation");
                break;
            case 2:
                var xenMobs = level is XenExperimentLevel.Dangerous or XenExperimentLevel.Cascade
                    ? console.Comp.DangerousXenMobs
                    : console.Comp.XenMobs;
                if (xenMobs.Count == 0)
                    return;

                var spawnCount = level switch
                {
                    XenExperimentLevel.Dangerous => 2,
                    XenExperimentLevel.Cascade => 3,
                    _ => 1
                };

                for (var i = 0; i < spawnCount; i++)
                    SpawnStationIncident(console, _random.Pick(xenMobs));

                ShowIncident(console, "xen-experiment-console-incident-xen-mob");
                break;
        }
    }

    private void SpawnStationIncident(
        Entity<XenExperimentConsoleComponent> console,
        EntProtoId prototype)
    {
        var station = _station.GetOwningStation(console);
        if (station != null && _station.GetLargestGrid(station.Value) is { } grid)
        {
            if (TryGetRandomStationCoordinates(grid, out var coordinates))
            {
                Spawn(prototype, coordinates);
                return;
            }
        }

        Spawn(prototype, Transform(console).Coordinates);
    }

    private bool TryGetRandomStationCoordinates(EntityUid grid, out EntityCoordinates coordinates)
    {
        coordinates = EntityCoordinates.Invalid;
        if (!TryComp<MapGridComponent>(grid, out var gridComponent))
            return false;

        var gridTransform = Transform(grid);
        var bounds = gridComponent.LocalAABB;
        var physicsQuery = GetEntityQuery<PhysicsComponent>();

        for (var i = 0; i < 25; i++)
        {
            var tile = new Vector2i(
                _random.Next((int) bounds.Left, (int) bounds.Right),
                _random.Next((int) bounds.Bottom, (int) bounds.Top));

            if (_atmosphere.IsTileSpace(grid, gridTransform.MapUid, tile) ||
                _atmosphere.IsTileAirBlocked(grid, tile, mapGridComp: gridComponent))
            {
                continue;
            }

            var blocked = false;
            foreach (var entity in _map.GetAnchoredEntities(grid, gridComponent, tile))
            {
                if (!physicsQuery.TryGetComponent(entity, out var body) ||
                    body.BodyType != BodyType.Static ||
                    !body.Hard ||
                    (body.CollisionLayer & (int) CollisionGroup.Impassable) == 0)
                {
                    continue;
                }

                blocked = true;
                break;
            }

            if (blocked)
                continue;

            coordinates = _map.GridTileToLocal(grid, gridComponent, tile);
            return true;
        }

        return false;
    }

    private void BurnOutCapacitor(Entity<XenExperimentConsoleComponent> console)
    {
        _itemSlots.SetLock(console, console.Comp.CapacitorSlotId, false);
        if (!_itemSlots.TryGetSlot(console, console.Comp.CapacitorSlotId, out var slot) ||
            slot.Item is not { Valid: true } capacitor)
        {
            return;
        }

        QueueDel(capacitor);
        _popup.PopupEntity(
            Loc.GetString("xen-experiment-console-capacitor-destroyed"),
            console,
            PopupType.LargeCaution);
    }

    private void UnlockExperimentSlots(Entity<XenExperimentConsoleComponent> console)
    {
        _itemSlots.SetLock(console, console.Comp.SampleSlotId, false);
        _itemSlots.SetLock(console, console.Comp.CapacitorSlotId, false);
    }

    private void UpdateUserInterface(Entity<XenExperimentConsoleComponent> console)
    {
        if (!console.Comp.Initialized)
            return;

        var connected = _research.TryGetClientServer(console, out _, out var serverComp);
        var data = connected && serverComp != null ? serverComp.Points : 0;
        string? samplePrototype = null;
        string? capacitorPrototype = null;
        var sampleProcessed = false;

        if (_itemSlots.TryGetSlot(console, console.Comp.SampleSlotId, out var slot) &&
            slot.Item is { Valid: true } sample)
        {
            samplePrototype = MetaData(sample).EntityPrototype?.ID;
            sampleProcessed = HasComp<XenExperimentResultComponent>(sample);
        }

        if (_itemSlots.TryGetSlot(console, console.Comp.CapacitorSlotId, out var capacitorSlot) &&
            capacitorSlot.Item is { Valid: true } capacitor)
        {
            capacitorPrototype = MetaData(capacitor).EntityPrototype?.ID;
        }

        var remainingSeconds = console.Comp.RunningExperiment == null
            ? 0
            : Math.Max(0, (int) Math.Ceiling((console.Comp.ExperimentEndTime - _timing.CurTime).TotalSeconds));

        _userInterface.SetUiState(
            console.Owner,
            XenExperimentConsoleUiKey.Key,
            new XenExperimentConsoleBoundUserInterfaceState(
                data,
                console.Comp.Stability,
                samplePrototype,
                sampleProcessed,
                capacitorPrototype,
                GetIncidentChanceMultiplier(console),
                console.Comp.RunningExperiment?.Id,
                remainingSeconds,
                this.IsPowered(console, EntityManager),
                connected));
    }

    private void RecordResult(
        Entity<XenExperimentConsoleComponent> console,
        XenExperimentPrototype experiment,
        XenExperimentResultQuality quality)
    {
        if (!_itemSlots.TryGetSlot(console, console.Comp.SampleSlotId, out var slot) ||
            slot.Item is not { Valid: true } sample)
        {
            _itemSlots.SetLock(console, console.Comp.SampleSlotId, false);
            return;
        }

        var result = EnsureComp<XenExperimentResultComponent>(sample);
        result.Experiment = experiment.ID;
        result.Quality = quality;
        result.RecordedData = GetRecordedData(experiment.Reward, quality);
        var baseMaterialAmount = TryComp<ScientificCrusherInputComponent>(sample, out var input)
            ? Math.Max(1, input.MaterialAmount)
            : 1;
        var materialMultiplier = quality switch
        {
            XenExperimentResultQuality.Analyzed => 2,
            XenExperimentResultQuality.Unstable => 3,
            XenExperimentResultQuality.Spoiled => 1,
            _ => 1
        };
        result.MaterialAmount = baseMaterialAmount * materialMultiplier;

        var resultName = quality switch
        {
            XenExperimentResultQuality.Analyzed => "xen-experiment-result-name-analyzed",
            XenExperimentResultQuality.Unstable => "xen-experiment-result-name-unstable",
            XenExperimentResultQuality.Spoiled => "xen-experiment-result-name-spoiled",
            _ => "xen-experiment-result-name-spoiled"
        };
        _metaData.SetEntityName(sample, Loc.GetString(resultName, ("sample", Name(sample))));
        _appearance.SetData(sample, XenExperimentResultVisuals.Quality, quality);

        _itemSlots.SetLock(console, slot, false);
    }

    private static int GetRecordedData(int reward, XenExperimentResultQuality quality)
    {
        var multiplier = quality switch
        {
            XenExperimentResultQuality.Analyzed => 1f,
            XenExperimentResultQuality.Unstable => 0.75f,
            XenExperimentResultQuality.Spoiled => 0.2f,
            _ => 0f
        };

        return (int) Math.Round(reward * multiplier);
    }

    private float GetIncidentChanceMultiplier(Entity<XenExperimentConsoleComponent> console)
    {
        if (!_itemSlots.TryGetSlot(console, console.Comp.CapacitorSlotId, out var slot) ||
            slot.Item is not { Valid: true } capacitor ||
            !TryComp<XenExperimentCapacitorComponent>(capacitor, out var capacitorComponent))
        {
            return 1f;
        }

        return Math.Clamp(capacitorComponent.IncidentChanceMultiplier, 0f, 1f);
    }

    private void ShowError(
        Entity<XenExperimentConsoleComponent> console,
        EntityUid user,
        string message,
        bool quiet)
    {
        if (!quiet)
            _popup.PopupEntity(Loc.GetString(message), console, user);
    }

    private void ShowIncident(Entity<XenExperimentConsoleComponent> console, string message)
    {
        _popup.PopupEntity(Loc.GetString(message), console, PopupType.LargeCaution);
        _chat.DispatchStationAnnouncement(
            console,
            Loc.GetString(message),
            Loc.GetString("xen-experiment-console-incident-announcement-sender"),
            playDefaultSound: false);
    }

}
