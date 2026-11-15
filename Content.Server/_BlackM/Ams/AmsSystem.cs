using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Content.Server._BlackM.Elevator;
using Content.Server._BlackM.ZenIntervention;
using Content.Server.Chat.Systems;
using Content.Server.Decals;
using Content.Server.Explosion.EntitySystems;
using Content.Server.Light.Components;
using Content.Server.Light.EntitySystems;
using Content.Server.RoundEnd;
using Content.Shared._BlackM.Ams;
using Content.Shared.Audio;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Movement.Pulling.Systems;
using Content.Shared.Popups;
using Content.Shared.Stacks;
using Content.Server._BlackM.Achievements;
using Content.Shared.Doors.Components;
using Content.Shared.Doors.Systems;
using Content.Shared.GameTicking;
using Content.Shared.Light.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._BlackM.Ams;

public sealed class AmsLaunchedEvent : EntityEventArgs
{
}

public sealed class AmsDetonatedEvent : EntityEventArgs
{
}

public sealed class AmsSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IPlayerManager _players = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;
    [Dependency] private readonly SharedAmbientSoundSystem _ambient = default!;
    [Dependency] private readonly SharedPointLightSystem _pointLight = default!;
    [Dependency] private readonly PoweredLightSystem _poweredLight = default!;
    [Dependency] private readonly ItemSlotsSystem _itemSlots = default!;
    [Dependency] private readonly BlackMElevatorSystem _elevator = default!;
    [Dependency] private readonly SharedDoorSystem _door = default!;
    [Dependency] private readonly ZenInterventionSystem _zenIntervention = default!;
    [Dependency] private readonly DecalSystem _decals = default!;
    [Dependency] private readonly ExplosionSystem _explosion = default!;
    [Dependency] private readonly RoundEndSystem _roundEnd = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly AmsRoundEndSystem _amsRoundEnd = default!;
    [Dependency] private readonly AmsAchievementsSystem _amsAchievements = default!;
    [Dependency] private readonly SharedStackSystem _stack = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly PullingSystem _pulling = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;

    private readonly Dictionary<(EntityUid Machine, EntityUid Part), EntityUid> _partInsertActors = new();

    private const string EvacuationElevatorId = "evac";
    private static readonly TimeSpan UiInterval = TimeSpan.FromMilliseconds(500);

    private static readonly SoundPathSpecifier HumIdle = new("/Audio/_BlackM/Ams/ams_hum_idle.ogg");
    private static readonly SoundPathSpecifier HumSync = new("/Audio/_BlackM/Ams/ams_hum_sync.ogg");
    private static readonly SoundPathSpecifier HumReady = new("/Audio/_BlackM/Ams/ams_hum_ready.ogg");
    private static readonly SoundPathSpecifier HumActive = new("/Audio/_BlackM/Ams/ams_hum_active.ogg");
    private static readonly SoundPathSpecifier HumCritical = new("/Audio/_BlackM/Ams/ams_hum_critical.ogg");
    private static readonly SoundPathSpecifier PowerUpSound = new("/Audio/_BlackM/Ams/ams_powerup.ogg");
    private static readonly SoundPathSpecifier ReadySound = new("/Audio/_BlackM/Ams/ams_calibrated.ogg");
    private static readonly SoundPathSpecifier InfectSound = new("/Audio/_BlackM/Ams/ams_infect.ogg");
    private static readonly SoundPathSpecifier CrackleSound = new("/Audio/_BlackM/Ams/ams_crackle.ogg");
    private static readonly SoundPathSpecifier ChargeSound = new("/Audio/_BlackM/Ams/ams_charge.ogg");
    private static readonly SoundPathSpecifier CascadeSound = new("/Audio/_BlackM/Ams/ams_cascade.ogg");
    private static readonly SoundPathSpecifier ExplosionSound = new("/Audio/_BlackM/Ams/ams_explosion.ogg");

    private static readonly SoundPathSpecifier SyncAnnounceSound = new("/Audio/_BlackM/Announcements/announcesync.ogg");
    private static readonly SoundPathSpecifier AnnounceSound = new("/Audio/_BlackM/Announcements/announceams.ogg");
    private static readonly SoundPathSpecifier HecuSound = new("/Audio/_BlackM/Announcements/hecucrack.ogg");
    private static readonly SoundPathSpecifier LeverSound = new("/Audio/Machines/machine_switch.ogg");
    private static readonly SoundPathSpecifier ReinforceSound = new("/Audio/Items/ratchet.ogg");

    private readonly record struct Look(SoundPathSpecifier Sound, float Volume, float Range, string Color, float Energy, float Radius);

    private static readonly Dictionary<AmsStatus, Look> Looks = new()
    {
        [AmsStatus.Inactive]      = new(HumIdle,     -10f,  4f, "#3ad1ff", 1.5f, 3f),
        [AmsStatus.Synchronizing] = new(HumSync,      -6f,  8f, "#3affb0", 2.5f, 5f),
        [AmsStatus.Ready]         = new(HumReady,     -5f, 10f, "#7fe6ff", 3.5f, 6f),
        [AmsStatus.Active]        = new(HumActive,    -3f, 14f, "#7fe6ff", 5f,   8f),
        [AmsStatus.Detonating]    = new(HumCritical,   0f, 18f, "#5dff3a", 6f,  10f),
    };

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);
        SubscribeLocalEvent<AmsComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<AmsComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<AmsComponent, EntInsertedIntoContainerMessage>(OnPartInserted);
        SubscribeLocalEvent<AmsComponent, ItemSlotInsertAttemptEvent>(OnPartInsertAttempt);
        SubscribeLocalEvent<AmsComponent, EntRemovedFromContainerMessage>(OnPartRemoved);
        SubscribeLocalEvent<AmsComponent, BoundUIOpenedEvent>(OnUiOpened);
        SubscribeLocalEvent<AmsComponent, AmsLaunchButtonMessage>(OnLaunchButton);
        SubscribeLocalEvent<AmsComponent, AmsLeverMessage>(OnLever);
        SubscribeLocalEvent<AmsComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<AmsComponent, AmsReinforceDoAfterEvent>(OnReinforceDoAfter);
    }

    private void OnRoundRestart(RoundRestartCleanupEvent ev)
    {
        _openedRandomDoors.Clear();
        _savedLightState.Clear();
        _savedEmergencyColors.Clear();
        _infections.Clear();
        _sequences.Clear();
        _blastParams.Clear();
    }

    private void OnMapInit(EntityUid uid, AmsComponent ams, MapInitEvent args)
    {
        ApplyStatusEffects(uid, ams);
    }

    private void OnShutdown(EntityUid uid, AmsComponent ams, ComponentShutdown args)
    {
        _infections.Remove(uid);

        if (_sequences.TryGetValue(uid, out var seq) && !seq.Blasting)
            _sequences.Remove(uid);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<AmsComponent>();
        while (query.MoveNext(out var uid, out var ams))
        {
            UpdateStatus(uid, ams);
            UpdateFlicker(uid, ams);
            UpdateUi(uid, ams);
        }

        UpdateInfections(frameTime);
        UpdateSequences();
    }

    #region status

    public void SetStatus(EntityUid uid, AmsComponent ams, AmsStatus status)
    {
        if (ams.Status == status)
            return;

        ams.Status = status;
        Dirty(uid, ams);
        ApplyStatusEffects(uid, ams);

        switch (status)
        {
            case AmsStatus.Synchronizing:
                StartInfection(uid, ams);
                break;
            case AmsStatus.Detonating:
                StartSequence(uid);
                break;
            case AmsStatus.Destroyed:
                _infections.Remove(uid);
                break;
        }
    }

    private void UpdateStatus(EntityUid uid, AmsComponent ams)
    {
        var now = _timing.CurTime;

        switch (ams.Status)
        {
            case AmsStatus.Inactive:
                if (ams.Primed && ams.RequireSample && now >= ams.NextSampleCheck)
                {
                    ams.NextSampleCheck = now + TimeSpan.FromSeconds(0.5);
                    if (FindSample(uid, ams) is { } sample)
                        StartSync(uid, ams, sample);
                }
                break;

            case AmsStatus.Synchronizing:
                if (ams.SyncStartTime is not { } start)
                    break;

                var elapsed = (float) (now - start).TotalSeconds;

                if (!ams.HecuSpawned && elapsed >= ams.HecuSpawnDelay)
                {
                    ams.HecuSpawned = true;
                    SpawnHecuSoldiers();
                    SendHecuAnnouncement();
                    OpenInfectedMaintDoors();
                }

                if (elapsed >= ams.ChargeTime)
                    ActivateField(uid, ams);

                break;

            case AmsStatus.Ready:
                ActivateField(uid, ams);
                break;

            case AmsStatus.Active:
                if (!ams.Launched)
                    Launch(uid, ams);

                if (ams.ActiveEndTime is not { } end)
                    break;

                var remaining = end - now;

                if (!ams.ClosingWarningSent && remaining <= TimeSpan.FromSeconds(15))
                {
                    ams.ClosingWarningSent = true;
                    Announce(Loc.GetString("ams-announce-closing-soon"), AnnounceSound, Color.Gray);
                }

                if (remaining <= TimeSpan.Zero)
                    BeginDetonation(uid, ams);

                break;

            case AmsStatus.Detonating:
                UpdatePortals(uid, ams, now);
                break;
        }
    }

    private void OnLaunchButton(EntityUid uid, AmsComponent ams, AmsLaunchButtonMessage args)
    {
        if (ams.Status != AmsStatus.Inactive || ams.RequireSample || !ams.Primed)
            return;

        StartSync(uid, ams, null);
    }

    private void StartSync(EntityUid uid, AmsComponent ams, EntityUid? sample)
    {
        ams.SyncStartTime = _timing.CurTime;
        ams.HecuSpawned = false;
        ams.Launched = false;

        if (sample is { } cart)
            LockSample(uid, ams, cart);

        SetStatus(uid, ams, AmsStatus.Synchronizing);
        Announce(Loc.GetString("ams-announce-sync-started", ("minutes", Minutes(ams.ChargeTime))),
            SyncAnnounceSound, Color.Gray);
        _audio.PlayPvs(PowerUpSound, uid, AudioParams.Default.WithMaxDistance(16f));
        TurnOffAllLights(uid);
        PushUi(uid, ams);
    }

    private void ActivateField(EntityUid uid, AmsComponent ams)
    {
        ams.ActiveEndTime = _timing.CurTime + TimeSpan.FromSeconds(ams.ActiveDuration);
        ams.ClosingWarningSent = false;
        ams.Launched = false;
        SetStatus(uid, ams, AmsStatus.Active);
        Launch(uid, ams);
    }

    private void Launch(EntityUid uid, AmsComponent ams)
    {
        if (ams.Launched)
            return;

        ams.Launched = true;
        ams.ActiveEndTime ??= _timing.CurTime + TimeSpan.FromSeconds(ams.ActiveDuration);

        Try("announce", () => Announce(Loc.GetString("ams-announce-launched", ("minutes", Minutes(ams.ActiveDuration))),
            AnnounceSound, Color.Gray));

        Try("evacuation", () =>
        {
            if (_elevator.SetEvacuation(EvacuationElevatorId, true))
                Announce(Loc.GetString("ams-announce-evac-available"), AnnounceSound, Color.Gray);
            else
                Log.Warning($"AMS: лифт эвакуации '{EvacuationElevatorId}' не найден или уже запущен.");
        });

        Try("launched event", () => RaiseLocalEvent(new AmsLaunchedEvent()));
        Try("sound", () => _audio.PlayPvs(ReadySound, uid, AudioParams.Default.WithMaxDistance(14f)));
        Try("lights", RestoreLights);

        PushUi(uid, ams);
    }

    private void Try(string what, Action action)
    {
        try
        {
            action();
        }
        catch (Exception e)
        {
            Log.Error($"AMS: ошибка на шаге '{what}': {e}");
        }
    }

    private static int Minutes(float seconds) => Math.Max(1, (int) MathF.Round(seconds / 60f));

    private void BeginDetonation(EntityUid uid, AmsComponent ams)
    {
        _elevator.SetEvacuation(EvacuationElevatorId, false);

        ams.ActiveEndTime = null;
        ams.DetonationTime = _timing.CurTime + TimeSpan.FromSeconds(ams.DetonationDelay);
        ams.NextPortal = _timing.CurTime + TimeSpan.FromSeconds(3);
        SetStatus(uid, ams, AmsStatus.Detonating);

        Announce(Loc.GetString("ams-announce-cascade-start", ("seconds", (int) ams.DetonationDelay)),
            AnnounceSound, Color.Red);
    }

    #endregion

    #region parts

    private void OnPartInsertAttempt(EntityUid uid, AmsComponent ams, ref ItemSlotInsertAttemptEvent args)
    {
        if (ams.Status != AmsStatus.Inactive || args.Slot.ID is not { } slotId ||
            !ams.PartSlots.Contains(slotId) || args.User is not { } actor)
            return;

        _partInsertActors[(uid, args.Item)] = actor;
    }

    private void OnPartRemoved(EntityUid uid, AmsComponent ams, EntRemovedFromContainerMessage args)
    {
        _partInsertActors.Remove((uid, args.Entity));
    }

    private int CountFilled(EntityUid uid, AmsComponent ams)
    {
        var count = 0;
        foreach (var id in ams.PartSlots)
        {
            if (_itemSlots.TryGetSlot(uid, id, out var slot) && slot.HasItem)
                count++;
        }

        return count;
    }

    private void OnPartInserted(EntityUid uid, AmsComponent ams, EntInsertedIntoContainerMessage args)
    {
        var slotId = args.Container.ID;
        if (ams.Status != AmsStatus.Inactive || !ams.PartSlots.Contains(slotId))
            return;

        if (_partInsertActors.Remove((uid, args.Entity), out var actor) &&
            HasComp<AmsPartComponent>(args.Entity))
        {
            _amsAchievements.OnPartInstalled(actor, args.Entity);
        }

        if (_itemSlots.TryGetSlot(uid, slotId, out var slot))
            _itemSlots.SetLock(uid, slot, true);

        ams.Reinforced[slotId] = 0;

        _zenIntervention.AdjustLevel(-ams.ZenReductionPerPart);
        OpenRandomPoolDoor();
        OpenLootRoomForSlot(slotId);

        if (!ams.SpecialDoorOpened && CountFilled(uid, ams) >= ams.SpecialDoorRequiredParts)
        {
            ams.SpecialDoorOpened = true;
            OpenSpecialDoor(ams);
        }

        UpdatePrimed(uid, ams);
        PushUi(uid, ams);
    }

    private bool IsInstalled(EntityUid uid, string slotId)
        => _itemSlots.TryGetSlot(uid, slotId, out var slot) && slot.HasItem;

    private int Needed(AmsComponent ams, string slotId)
        => ams.Reinforcements.TryGetValue(slotId, out var r) ? r.Amount : 0;

    private bool IsSecured(EntityUid uid, AmsComponent ams, string slotId)
        => IsInstalled(uid, slotId) && ams.Reinforced.GetValueOrDefault(slotId) >= Needed(ams, slotId);

    private string? FindSlotForStack(EntityUid uid, AmsComponent ams, string stackType)
    {
        foreach (var slotId in ams.PartSlots)
        {
            if (!ams.Reinforcements.TryGetValue(slotId, out var r) || r.Stack != stackType)
                continue;

            if (IsInstalled(uid, slotId) && !IsSecured(uid, ams, slotId))
                return slotId;
        }

        return null;
    }

    private void OnInteractUsing(EntityUid uid, AmsComponent ams, InteractUsingEvent args)
    {
        if (args.Handled || !TryComp<StackComponent>(args.Used, out var stack))
            return;

        if (ams.Status != AmsStatus.Inactive)
            return;

        if (FindSlotForStack(uid, ams, stack.StackTypeId) is not { } slotId)
        {
            foreach (var r in ams.Reinforcements.Values)
            {
                if (r.Stack != stack.StackTypeId)
                    continue;

                _popup.PopupEntity(Loc.GetString("ams-popup-nothing-to-reinforce"), uid, args.User);
                args.Handled = true;
                return;
            }

            return;
        }

        args.Handled = true;

        var doAfter = new DoAfterArgs(EntityManager, args.User, ams.ReinforceDelay, new AmsReinforceDoAfterEvent(),
            uid, target: uid, used: args.Used)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            NeedHand = true,
        };

        if (_doAfter.TryStartDoAfter(doAfter))
            _audio.PlayPvs(ReinforceSound, uid);
    }

    private void OnReinforceDoAfter(EntityUid uid, AmsComponent ams, AmsReinforceDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Used is not { } used || ams.Status != AmsStatus.Inactive)
            return;

        if (!TryComp<StackComponent>(used, out var stack) ||
            FindSlotForStack(uid, ams, stack.StackTypeId) is not { } slotId)
            return;

        args.Handled = true;

        var need = Needed(ams, slotId) - ams.Reinforced.GetValueOrDefault(slotId);
        var take = Math.Min(need, stack.Count);
        if (take <= 0 || !_stack.Use(used, take, stack))
            return;

        ams.Reinforced[slotId] = ams.Reinforced.GetValueOrDefault(slotId) + take;

        var part = Loc.GetString(slotId.Replace('_', '-'));
        if (IsSecured(uid, ams, slotId))
        {
            _popup.PopupEntity(Loc.GetString("ams-popup-secured", ("part", part)), uid, args.User);
            _audio.PlayPvs(ReinforceSound, uid);
        }
        else
        {
            _popup.PopupEntity(Loc.GetString("ams-popup-reinforce-progress", ("part", part),
                ("left", Needed(ams, slotId) - ams.Reinforced[slotId])), uid, args.User);
        }

        PushUi(uid, ams);
    }

    private void OnLever(EntityUid uid, AmsComponent ams, AmsLeverMessage args)
    {
        if (ams.Status != AmsStatus.Inactive || !ams.PartSlots.Contains(args.Slot))
            return;

        if (!ams.Levers.Contains(args.Slot) && !IsSecured(uid, ams, args.Slot))
            return;

        if (!ams.Levers.Add(args.Slot))
            ams.Levers.Remove(args.Slot);

        _audio.PlayPvs(LeverSound, uid, AudioParams.Default.WithVolume(-2f));
        UpdatePrimed(uid, ams);
        PushUi(uid, ams);
    }

    private void UpdatePrimed(EntityUid uid, AmsComponent ams)
    {
        var primed = ams.Status == AmsStatus.Inactive;
        foreach (var slotId in ams.PartSlots)
        {
            if (!IsSecured(uid, ams, slotId) || !ams.Levers.Contains(slotId))
            {
                primed = false;
                break;
            }
        }

        if (ams.Primed == primed)
            return;

        ams.Primed = primed;
        Dirty(uid, ams);

        if (primed)
        {
            _audio.PlayPvs(ChargeSound, uid, AudioParams.Default.WithVolume(-6f).WithMaxDistance(12f));
            Announce(Loc.GetString(ams.RequireSample ? "ams-announce-primed-sample" : "ams-announce-primed"),
                AnnounceSound, Color.Gray);
        }
    }

    private EntityUid? FindSample(EntityUid uid, AmsComponent ams)
    {
        var spot = Transform(uid).Coordinates.Offset(ams.SampleOffset);
        foreach (var ent in _lookup.GetEntitiesInRange<AmsSampleComponent>(spot, ams.SampleRange))
        {
            if (!ent.Comp.Used)
                return ent.Owner;
        }

        return null;
    }

    private void LockSample(EntityUid uid, AmsComponent ams, EntityUid cart)
    {
        if (!TryComp<AmsSampleComponent>(cart, out var sample))
            return;

        sample.Used = true;
        ams.Sample = cart;

        if (TryComp<PullableComponent>(cart, out var pullable) && pullable.BeingPulled)
            _pulling.TryStopPull(cart, pullable);
        _transform.AnchorEntity(cart, Transform(cart));
        _appearance.SetData(cart, AmsSampleVisuals.Active, true);

        if (TryComp<PointLightComponent>(cart, out var light))
        {
            _pointLight.SetEnergy(cart, 4f, light);
            _pointLight.SetRadius(cart, 3f, light);
        }
    }

    #endregion

    #region UI

    private void OnUiOpened(EntityUid uid, AmsComponent ams, BoundUIOpenedEvent args)
    {
        PushUi(uid, ams);
    }

    private void UpdateUi(EntityUid uid, AmsComponent ams)
    {
        var now = _timing.CurTime;
        if (now < ams.NextUiUpdate)
            return;

        ams.NextUiUpdate = now + UiInterval;

        if (_ui.IsUiOpen(uid, AmsUiKey.Key))
            PushUi(uid, ams);
    }

    private void PushUi(EntityUid uid, AmsComponent ams)
    {
        var parts = new List<AmsPartUiData>();
        foreach (var id in ams.PartSlots)
        {
            string? material = null;
            var needed = 0;
            if (ams.Reinforcements.TryGetValue(id, out var r))
            {
                material = Loc.GetString($"ams-material-{r.Stack}");
                needed = r.Amount;
            }

            parts.Add(new AmsPartUiData(id, IsInstalled(uid, id), material,
                ams.Reinforced.GetValueOrDefault(id), needed, ams.Levers.Contains(id)));
        }

        var sampleInPlace = ams.Sample != null ||
                            (ams.Status == AmsStatus.Inactive && ams.RequireSample && FindSample(uid, ams) != null);

        _ui.SetUiState(uid, AmsUiKey.Key,
            new AmsBuiState(ams.Status, ams.SyncStartTime, ams.ActiveEndTime, ams.DetonationTime,
                ams.ChargeTime, parts, ams.RequireSample, sampleInPlace, ams.Primed));
    }

    #endregion

    #region Ambient

    private void ApplyStatusEffects(EntityUid uid, AmsComponent ams)
    {
        if (Looks.TryGetValue(ams.Status, out var look))
        {
            _ambient.SetSound(uid, look.Sound);
            _ambient.SetVolume(uid, look.Volume);
            _ambient.SetRange(uid, look.Range);
            _ambient.SetAmbience(uid, true);

            _pointLight.SetEnabled(uid, true);
            _pointLight.SetColor(uid, Color.FromHex(look.Color));
            _pointLight.SetEnergy(uid, look.Energy);
            _pointLight.SetRadius(uid, look.Radius);
        }
        else
        {
            _ambient.SetAmbience(uid, false);
            _pointLight.SetEnabled(uid, false);
        }

    }

    private void UpdateFlicker(EntityUid uid, AmsComponent ams)
    {
        if (ams.Status != AmsStatus.Detonating)
            return;

        var now = _timing.CurTime;
        if (now < ams.NextFlicker)
            return;

        ams.NextFlicker = now + TimeSpan.FromSeconds(_random.NextFloat(0.08f, 0.3f));
        _pointLight.SetEnergy(uid, _random.NextFloat(2f, 9f));
        _pointLight.SetColor(uid, _random.Prob(0.25f) ? Color.White : Color.FromHex("#5dff3a"));
    }

    #endregion

    #region door hecu light

    private const string InfectedMaintDoorPrototype = "AirlockMaintInfectedBlackM";
    private const string RandomDoorGroup = "ams_random";
    private const string SpecialDoorGroup = "ams_special";
    private const string LootDoorGroupPrefix = "ams_loot_";

    private readonly HashSet<EntityUid> _openedRandomDoors = new();
    private readonly Dictionary<EntityUid, bool> _savedLightState = new();
    private readonly Dictionary<EntityUid, Color> _savedEmergencyColors = new();

    private void SpawnHecuSoldiers()
    {
        var query = EntityQueryEnumerator<HecuSpawnPointComponent>();
        while (query.MoveNext(out var spawnUid, out var spawnPoint))
        {
            Spawn(spawnPoint.Prototype, Transform(spawnUid).Coordinates);
        }
    }

    private void SendHecuAnnouncement()
    {
        _chat.DispatchGlobalAnnouncement(
            Loc.GetString("ams-announce-hecu-intercept"),
            sender: Loc.GetString("ams-announce-hecu-sender"),
            playSound: true,
            announcementSound: HecuSound,
            colorOverride: Color.DarkGreen);
    }

    private void OpenInfectedMaintDoors()
    {
        var query = EntityQueryEnumerator<DoorComponent, MetaDataComponent>();
        while (query.MoveNext(out var uid, out var door, out var meta))
        {
            if (meta.EntityPrototype?.ID != InfectedMaintDoorPrototype)
                continue;

            _door.StartOpening(uid, door);

            if (TryComp<DoorBoltComponent>(uid, out var bolt))
                _door.SetBoltsDown((uid, bolt), true, null, false);
        }
    }

    private void OpenRandomPoolDoor()
    {
        var candidates = new List<EntityUid>();
        var query = EntityQueryEnumerator<AmsControlledDoorComponent>();
        while (query.MoveNext(out var doorUid, out var tag))
        {
            if (tag.GroupId == RandomDoorGroup && !_openedRandomDoors.Contains(doorUid))
                candidates.Add(doorUid);
        }

        if (candidates.Count == 0)
            return;

        var chosen = _random.Pick(candidates);
        _openedRandomDoors.Add(chosen);
        OpenDoor(chosen);
        Announce(Loc.GetString("ams-announce-random-door"), AnnounceSound, Color.Orange);
    }

    private void OpenSpecialDoor(AmsComponent ams)
    {
        var query = EntityQueryEnumerator<AmsControlledDoorComponent>();
        while (query.MoveNext(out var doorUid, out var tag))
        {
            if (tag.GroupId != SpecialDoorGroup)
                continue;

            OpenDoor(doorUid);
            Announce(Loc.GetString("ams-announce-special-door",
                ("count", ams.SpecialDoorRequiredParts), ("total", ams.PartSlots.Count)), AnnounceSound, Color.Orange);
            return;
        }
    }

    private void OpenLootRoomForSlot(string slotId)
    {
        var groupId = LootDoorGroupPrefix + slotId;

        var query = EntityQueryEnumerator<AmsControlledDoorComponent>();
        while (query.MoveNext(out var doorUid, out var tag))
        {
            if (tag.GroupId != groupId)
                continue;

            OpenDoor(doorUid);

            var location = string.IsNullOrWhiteSpace(tag.AnnouncementLocationName) ? slotId : tag.AnnouncementLocationName;
            Announce(Loc.GetString("ams-announce-loot-room", ("location", location)), AnnounceSound, Color.Orange);
            return;
        }
    }

    private void OpenDoor(EntityUid uid)
    {
        if (TryComp<DoorBoltComponent>(uid, out var bolt))
            _door.SetBoltsDown((uid, bolt), false, null, false);

        if (TryComp<DoorComponent>(uid, out var door))
            _door.StartOpening(uid, door);
    }

    private void TurnOffAllLights(EntityUid portalUid)
    {
        _savedLightState.Clear();
        _savedEmergencyColors.Clear();

        var grid = Transform(portalUid).GridUid;
        if (grid == null)
            return;

        var lamps = EntityQueryEnumerator<PoweredLightComponent>();
        while (lamps.MoveNext(out var uid, out var lamp))
        {
            if (Transform(uid).GridUid != grid || HasComp<EmergencyLightComponent>(uid))
                continue;

            _savedLightState[uid] = lamp.On;

            if (lamp.On)
                _poweredLight.SetState(uid, false, lamp);
        }

        var emergency = EntityQueryEnumerator<EmergencyLightComponent, PointLightComponent>();
        while (emergency.MoveNext(out var eUid, out _, out var eLight))
        {
            if (Transform(eUid).GridUid != grid)
                continue;

            _savedEmergencyColors[eUid] = eLight.Color;
            _pointLight.SetEnabled(eUid, true, eLight);
            _pointLight.SetColor(eUid, Color.Red, eLight);
        }
    }

    private void RestoreLights()
    {
        var lamps = EntityQueryEnumerator<PoweredLightComponent>();
        while (lamps.MoveNext(out var uid, out var lamp))
        {
            if (!_savedLightState.TryGetValue(uid, out var wasOn))
                continue;

            if (wasOn && !lamp.On)
                _poweredLight.SetState(uid, true, lamp);
            else if (!wasOn && lamp.On)
                _poweredLight.SetState(uid, false, lamp);
        }

        var emergency = EntityQueryEnumerator<EmergencyLightComponent, PointLightComponent>();
        while (emergency.MoveNext(out var eUid, out _, out var eLight))
        {
            if (_savedEmergencyColors.TryGetValue(eUid, out var color))
                _pointLight.SetColor(eUid, color, eLight);
        }

        _savedLightState.Clear();
        _savedEmergencyColors.Clear();
    }

    private void Announce(string message, SoundPathSpecifier sound, Color color)
    {
        _chat.DispatchGlobalAnnouncement(
            message,
            sender: Loc.GetString("ams-announce-sender"),
            playSound: true,
            announcementSound: sound,
            colorOverride: color);
    }

    #endregion

    #region infect

    private sealed class Infection
    {
        public EntityUid Grid;
        public readonly List<Vector2i> Frontier = new();
        public readonly HashSet<Vector2i> Seen = new();
        public float Accumulator;
    }

    private const int InfectionFrontierWindow = 24;
    private const int InfectionMaxPerTick = 40;
    private static readonly (int X, int Y)[] Neighbors = { (1, 0), (-1, 0), (0, 1), (0, -1) };

    private readonly Dictionary<EntityUid, Infection> _infections = new();

    private void StartInfection(EntityUid uid, AmsComponent ams)
    {
        if (_infections.ContainsKey(uid) || ams.InfectionDecals.Count == 0)
            return;

        var xform = Transform(uid);
        if (xform.GridUid is not { } gridUid || !TryComp<MapGridComponent>(gridUid, out var grid))
            return;

        var origin = _map.TileIndicesFor(gridUid, grid, xform.Coordinates);
        var state = new Infection { Grid = gridUid };
        state.Frontier.Add(origin);
        state.Seen.Add(origin);
        _infections[uid] = state;
    }

    private void UpdateInfections(float frameTime)
    {
        if (_infections.Count == 0)
            return;

        var now = _timing.CurTime;

        foreach (var (uid, state) in _infections)
        {
            if (!TryComp<AmsComponent>(uid, out var ams) || !TryComp<MapGridComponent>(state.Grid, out var grid))
                continue;

            var rate = ams.Status switch
            {
                AmsStatus.Synchronizing => MathHelper.Lerp(ams.InfectionStartRate, ams.InfectionEndRate,
                    AmsHelpers.GetCharge(ams.Status, ams.SyncStartTime, ams.ChargeTime, now)),
                AmsStatus.Ready or AmsStatus.Active => ams.InfectionEndRate,
                AmsStatus.Detonating => ams.InfectionEndRate * 1.5f,
                _ => 0f,
            };

            state.Accumulator += rate * frameTime;
            var count = Math.Min((int) state.Accumulator, InfectionMaxPerTick);
            state.Accumulator -= (int) state.Accumulator;

            for (var i = 0; i < count && state.Frontier.Count > 0; i++)
                InfectStep(state, ams, grid);
        }
    }

    private void InfectStep(Infection state, AmsComponent ams, MapGridComponent grid)
    {
        var idx = _random.Next(Math.Min(state.Frontier.Count, InfectionFrontierWindow));
        var pos = state.Frontier[idx];
        state.Frontier.RemoveAt(idx);

        if (_map.GetTileRef(state.Grid, grid, pos).Tile.IsEmpty)
            return;

        var coords = new EntityCoordinates(state.Grid, new Vector2(pos.X * grid.TileSize, pos.Y * grid.TileSize));
        _decals.TryAddDecal(_random.Pick(ams.InfectionDecals), coords, out _,
            rotation: Angle.FromDegrees(90 * _random.Next(4)));

        if (_random.Prob(0.015f))
            _audio.PlayPvs(InfectSound, _map.GridTileToLocal(state.Grid, grid, pos),
                AudioParams.Default.WithVolume(-4f).WithMaxDistance(10f));

        foreach (var (dx, dy) in Neighbors)
        {
            var next = new Vector2i(pos.X + dx, pos.Y + dy);
            if (state.Seen.Add(next))
                state.Frontier.Add(next);
        }
    }

    #endregion

    #region nuke

    private sealed class Sequence
    {
        public MapId Map;
        public MapCoordinates Epicenter;
        public EntityCoordinates EpicenterLocal;

        public TimeSpan NextPulse;
        public bool Blasting;
        public bool Warned30, Warned10, ChargePlayed;

        public TimeSpan BlastStart;
        public TimeSpan LastTick;
        public TimeSpan NextKill;
        public float MaxDist;
        public float Budget;

        public readonly List<(MapCoordinates Pos, float Dist)> Points = new();
        public readonly List<(EntityUid Grid, Vector2 Local, float Dist)> Scorch = new();
        public readonly List<(EntityUid Light, float Dist)> Lights = new();
        public int PointIndex, ScorchIndex, LightIndex;
        public float NextRumble;
    }

    private const int ScorchPerTick = 60;
    private const float KillInterval = 0.25f;
    private const float RumbleStep = 18f;

    private readonly Dictionary<EntityUid, Sequence> _sequences = new();

    private void StartSequence(EntityUid uid)
    {
        if (_sequences.ContainsKey(uid))
            return;

        var xform = Transform(uid);
        _sequences[uid] = new Sequence
        {
            Map = xform.MapID,
            Epicenter = _transform.GetMapCoordinates(uid, xform),
            EpicenterLocal = xform.Coordinates,
            NextPulse = _timing.CurTime,
        };
    }

    private void UpdateSequences()
    {
        if (_sequences.Count == 0)
            return;

        var now = _timing.CurTime;

        foreach (var (uid, seq) in _sequences.ToArray())
        {
            if (!seq.Blasting)
            {
                if (!TryComp<AmsComponent>(uid, out var ams))
                {
                    _sequences.Remove(uid);
                    continue;
                }

                UpdateCountdown(uid, ams, seq, now);
            }
            else
            {
                UpdateBlast(uid, seq, now);
            }
        }
    }

    private void UpdateCountdown(EntityUid uid, AmsComponent ams, Sequence seq, TimeSpan now)
    {
        var remaining = (float) ((ams.DetonationTime ?? now) - now).TotalSeconds;

        if (!seq.Warned30 && remaining <= 30f)
        {
            seq.Warned30 = true;
            Announce(Loc.GetString("ams-announce-cascade-30"), AnnounceSound, Color.OrangeRed);
        }

        if (!seq.Warned10 && remaining <= 10f)
        {
            seq.Warned10 = true;
            Announce(Loc.GetString("ams-announce-cascade-10"), AnnounceSound, Color.Red);
        }

        if (!seq.ChargePlayed && remaining <= 12f)
        {
            seq.ChargePlayed = true;
            _audio.PlayGlobal(ChargeSound, Filter.BroadcastMap(seq.Map), false, AudioParams.Default.WithVolume(2f));
        }

        if (now >= seq.NextPulse)
        {
            var progress = AmsHelpers.GetDetonationProgress(ams, now);
            var offset = _random.NextAngle().ToVec() * _random.NextFloat(1f, 2f + 8f * progress);
            _audio.PlayPvs(CrackleSound, seq.EpicenterLocal.Offset(offset),
                AudioParams.Default.WithMaxDistance(16f).WithVolume(-2f + 4f * progress));

            seq.NextPulse = now + TimeSpan.FromSeconds(MathHelper.Lerp(1.6f, 0.3f, progress));
        }

        if (remaining <= 0f)
            StartBlast(uid, ams, seq, now);
    }

    private void StartBlast(EntityUid uid, AmsComponent ams, Sequence seq, TimeSpan now)
    {
        seq.Blasting = true;
        seq.BlastStart = now;
        seq.LastTick = now;
        seq.NextKill = now;

        SetStatus(uid, ams, AmsStatus.Destroyed);
        RaiseLocalEvent(new AmsDetonatedEvent());

        _audio.PlayGlobal(ExplosionSound, Filter.BroadcastMap(seq.Map), false, AudioParams.Default.WithVolume(4f));

        _explosion.QueueExplosion(seq.Epicenter, ams.ExplosionType, ams.CoreIntensity, ams.CoreSlope,
            ams.CoreMaxTileIntensity, null);

        PrepareBlast(ams, seq);

        _blastParams[uid] = (ams.ExplosionType, ams.WaveIntensity, ams.WaveSlope, ams.WaveMaxTileIntensity,
            ams.WaveSpeed, ams.WaveExplosionsPerSecond, ams.RoundEndDelay, ams.InfectionDecals.ToArray());

        RaiseNetworkEvent(new AmsBlastEvent((int) seq.Map, seq.Epicenter.Position, ams.WaveSpeed,
            seq.MaxDist + 2f, now), Filter.BroadcastMap(seq.Map));

        _infections.Remove(uid);
        QueueDel(uid);
    }

    private readonly Dictionary<EntityUid, (string Type, float Intensity, float Slope, float MaxTile, float Speed,
        float PerSecond, float RoundEndDelay, string[] Decals)> _blastParams = new();

    private void PrepareBlast(AmsComponent ams, Sequence seq)
    {
        var epicenter = seq.Epicenter.Position;
        var spacing = Math.Max(1, ams.WaveExplosionSpacing);
        var max = 1f;

        var grids = EntityQueryEnumerator<MapGridComponent, TransformComponent>();
        while (grids.MoveNext(out var gridUid, out var grid, out var gridXform))
        {
            if (gridXform.MapID != seq.Map)
                continue;

            foreach (var tile in _map.GetAllTiles(gridUid, grid))
            {
                var idx = tile.GridIndices;
                var local = new Vector2(idx.X * grid.TileSize, idx.Y * grid.TileSize);
                var world = _map.GridTileToWorldPos(gridUid, grid, idx);
                var dist = (world - epicenter).Length();
                max = MathF.Max(max, dist);

                if (Mod(idx.X, spacing) == 0 && Mod(idx.Y, spacing) == 0 && _random.Prob(ams.WaveExplosionChance))
                    seq.Points.Add((new MapCoordinates(world, seq.Map), dist));

                if (ams.InfectionDecals.Count > 0 && _random.Prob(ams.WaveScorchChance))
                    seq.Scorch.Add((gridUid, local, dist));
            }
        }

        seq.MaxDist = max;
        seq.Points.Sort((a, b) => a.Dist.CompareTo(b.Dist));
        seq.Scorch.Sort((a, b) => a.Dist.CompareTo(b.Dist));

        var lamps = EntityQueryEnumerator<PoweredLightComponent, TransformComponent>();
        while (lamps.MoveNext(out var lamp, out _, out var xform))
        {
            if (xform.MapID != seq.Map)
                continue;

            seq.Lights.Add((lamp, (_transform.GetWorldPosition(xform) - epicenter).Length()));
        }

        seq.Lights.Sort((a, b) => a.Dist.CompareTo(b.Dist));
    }

    private static int Mod(int a, int m) => ((a % m) + m) % m;

    private void UpdateBlast(EntityUid uid, Sequence seq, TimeSpan now)
    {
        if (!_blastParams.TryGetValue(uid, out var p))
        {
            _sequences.Remove(uid);
            return;
        }

        var dt = (float) (now - seq.LastTick).TotalSeconds;
        seq.LastTick = now;

        var radius = p.Speed * (float) (now - seq.BlastStart).TotalSeconds;

        seq.Budget = MathF.Min(seq.Budget + p.PerSecond * dt, p.PerSecond);
        while (seq.PointIndex < seq.Points.Count && seq.Points[seq.PointIndex].Dist <= radius && seq.Budget >= 1f)
        {
            var (pos, _) = seq.Points[seq.PointIndex++];
            seq.Budget -= 1f;
            _explosion.QueueExplosion(pos, p.Type, p.Intensity, p.Slope, p.MaxTile, null);
        }

        var scorched = 0;
        while (seq.ScorchIndex < seq.Scorch.Count && seq.Scorch[seq.ScorchIndex].Dist <= radius &&
               scorched++ < ScorchPerTick)
        {
            var (grid, local, _) = seq.Scorch[seq.ScorchIndex++];
            if (!Exists(grid))
                continue;

            _decals.TryAddDecal(_random.Pick(p.Decals), new EntityCoordinates(grid, local), out _,
                rotation: Angle.FromDegrees(90 * _random.Next(4)), cleanable: false);
        }

        while (seq.LightIndex < seq.Lights.Count && seq.Lights[seq.LightIndex].Dist <= radius)
        {
            var (light, _) = seq.Lights[seq.LightIndex++];
            if (Exists(light))
                _poweredLight.TryDestroyBulb(light);
        }

        if (radius >= seq.NextRumble && radius <= seq.MaxDist)
        {
            seq.NextRumble = radius + RumbleStep;
            foreach (var session in _players.Sessions)
            {
                if (session.AttachedEntity is not { } ent || Transform(ent).MapID != seq.Map)
                    continue;

                var dist = (_transform.GetWorldPosition(ent) - seq.Epicenter.Position).Length();
                if (MathF.Abs(dist - radius) < RumbleStep * 1.5f)
                    _audio.PlayEntity(CascadeSound, ent, ent, AudioParams.Default.WithVolume(-2f));
            }
        }

        if (now >= seq.NextKill)
        {
            seq.NextKill = now + TimeSpan.FromSeconds(KillInterval);
            KillInRadius(seq, radius);
        }

        var done = radius >= seq.MaxDist + 2f &&
                   seq.PointIndex >= seq.Points.Count &&
                   seq.ScorchIndex >= seq.Scorch.Count;

        if (done)
            Finish(uid, seq, p.RoundEndDelay);
    }

    private void Finish(EntityUid uid, Sequence seq, float roundEndDelay)
    {
        KillInRadius(seq, float.MaxValue);
        Announce(Loc.GetString("ams-announce-detonated"), AnnounceSound, Color.Red);

        _roundEnd.EndRound(TimeSpan.FromSeconds(roundEndDelay));

        _sequences.Remove(uid);
        _blastParams.Remove(uid);
    }

    private void KillInRadius(Sequence seq, float radius)
    {
        var epicenter = seq.Epicenter.Position;
        var query = EntityQueryEnumerator<MobStateComponent, TransformComponent>();
        while (query.MoveNext(out var mob, out var state, out var xform))
        {
            if (xform.MapID != seq.Map || state.CurrentState == MobState.Dead || _amsRoundEnd.IsEvacuated(mob))
                continue;

            if (radius < float.MaxValue && (_transform.GetWorldPosition(xform) - epicenter).Length() > radius)
                continue;

            _mobState.ChangeMobState(mob, MobState.Dead, state);
        }
    }

    #endregion

    #region portals

    private const int PortalAttempts = 8;

    private void UpdatePortals(EntityUid uid, AmsComponent ams, TimeSpan now)
    {
        if (now < ams.NextPortal)
            return;

        var progress = AmsHelpers.GetDetonationProgress(ams, now);
        ams.NextPortal = now + TimeSpan.FromSeconds(
            MathHelper.Lerp(ams.PortalIntervalStart, ams.PortalIntervalEnd, progress) * _random.NextFloat(0.7f, 1.3f));

        var map = Transform(uid).MapID;
        var targets = new List<EntityUid>();
        foreach (var session in _players.Sessions)
        {
            if (session.AttachedEntity is not { } ent || Transform(ent).MapID != map || !_mobState.IsAlive(ent) ||
                _amsRoundEnd.IsEvacuated(ent))
                continue;

            targets.Add(ent);
        }

        if (targets.Count == 0)
            return;

        var target = _random.Pick(targets);
        if (FindPortalSpot(target) is not { } spot)
            return;

        Spawn(ams.PortalPrototype, spot);

        if (ams.PortalSpawns.Count > 0 && _random.Prob(ams.PortalSpawnChance))
            Spawn(_random.Pick(ams.PortalSpawns), spot);
    }

    private EntityCoordinates? FindPortalSpot(EntityUid target)
    {
        var xform = Transform(target);
        if (xform.GridUid is not { } gridUid || !TryComp<MapGridComponent>(gridUid, out var grid))
            return null;

        var center = _map.TileIndicesFor(gridUid, grid, xform.Coordinates);

        for (var i = 0; i < PortalAttempts; i++)
        {
            var offset = _random.NextAngle().ToVec() * _random.NextFloat(3f, 10f);
            var idx = center + new Vector2i((int) MathF.Round(offset.X), (int) MathF.Round(offset.Y));

            if (!_map.TryGetTileRef(gridUid, grid, idx, out var tile) || tile.Tile.IsEmpty)
                continue;

            if (IsTileBlocked(gridUid, grid, idx))
                continue;

            return _map.GridTileToLocal(gridUid, grid, idx);
        }

        return null;
    }

    private bool IsTileBlocked(EntityUid gridUid, MapGridComponent grid, Vector2i idx)
    {
        var anchored = _map.GetAnchoredEntitiesEnumerator(gridUid, grid, idx);
        while (anchored.MoveNext(out var ent))
        {
            if (TryComp<OccluderComponent>(ent, out var occ) && occ.Enabled)
                return true;
        }

        return false;
    }

    #endregion
}
