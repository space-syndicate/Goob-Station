// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Numerics;
using Content.Shared._DV.Salvage.Systems;
using Content.Shared.Electrocution;
using Content.Shared.Emag.Systems;
using Content.Shared.EntityTable;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Lock;
using Content.Shared.Popups;
using Content.Shared.Power;
using Content.Shared.Power.Components;
using Content.Shared.Power.EntitySystems;
using Content.Shared.Storage.Components;
using Content.Shared.Storage.EntitySystems;
using Content.Shared.Verbs;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Goobstation.Shared.MiningCrate;

/// <summary>
/// Mining crate: power, unlock (points / free timer), visuals, loot, access breaker.
/// Security is handled by <see cref="SharedMiningCrateSecuritySystem"/>.
/// </summary>
public sealed class MiningCrateSystem : EntitySystem
{
    [Dependency] private readonly EmagSystem _emag = default!;
    [Dependency] private readonly EntityTableSystem _entityTable = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly LockSystem _lock = default!;
    [Dependency] private readonly MiningPointsSystem _miningPoints = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedElectrocutionSystem _electrocution = default!;
    [Dependency] private readonly SharedPointLightSystem _lights = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedPowerReceiverSystem _power = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MiningCrateComponent, ActivateInWorldEvent>(OnActivate,
            before: [typeof(LockSystem), typeof(SharedEntityStorageSystem)]);
        SubscribeLocalEvent<MiningCrateComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<MiningCrateComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<MiningCrateComponent, GetVerbsEvent<ActivationVerb>>(OnGetVerbs);
        SubscribeLocalEvent<MiningCrateComponent, LockToggleAttemptEvent>(OnLockToggleAttempt);
        SubscribeLocalEvent<MiningCrateComponent, StorageOpenAttemptEvent>(OnStorageOpenAttempt);
        SubscribeLocalEvent<MiningCrateComponent, StorageAfterOpenEvent>(OnStorageAfterOpen);
        SubscribeLocalEvent<MiningCrateComponent, StorageAfterCloseEvent>(OnStorageAfterClose);
        SubscribeLocalEvent<MiningCrateComponent, PowerChangedEvent>(OnPowerChanged);
        SubscribeLocalEvent<MiningCrateComponent, GotEmaggedEvent>(OnEmagged,
            before: [typeof(LockSystem)]);

        // Points first so free timer only runs when no payment component.
        SubscribeLocalEvent<MiningCratePointsUnlockComponent, MiningCrateTryUnlockEvent>(OnPointsTryUnlock);
        SubscribeLocalEvent<MiningCrateUnlockTimerComponent, MiningCrateTryUnlockEvent>(OnTimerTryUnlock);
    }

    private void OnMapInit(Entity<MiningCrateComponent> ent, ref MapInitEvent args)
    {
        if (ent.Comp.StartUnlocked)
            ApplyStartUnlocked(ent);

        UpdateCrateVisuals(ent);
    }

    private void ApplyStartUnlocked(Entity<MiningCrateComponent> ent)
    {
        if (!TryComp<LockComponent>(ent, out var lockComp))
            return;

        CompleteUnlock(ent, lockComp, user: null, playFeedback: false);
    }

    private void OnStorageAfterOpen(Entity<MiningCrateComponent> ent, ref StorageAfterOpenEvent args)
    {
        UpdateCrateVisuals(ent);
    }

    private void OnStorageAfterClose(Entity<MiningCrateComponent> ent, ref StorageAfterCloseEvent args)
    {
        UpdateCrateVisuals(ent);
    }

    private void OnPowerChanged(Entity<MiningCrateComponent> ent, ref PowerChangedEvent args)
    {
        SyncPhysicalSiren(ent.Owner);
        UpdateCrateVisuals(ent);
    }

    public override void Update(float frameTime)
    {
        if (_net.IsClient)
            return;

        UpdateDenyLockouts();
        UpdateUnlockTimers();
        UpdateTamperBlink();
    }

    private void UpdateDenyLockouts()
    {
        var query = EntityQueryEnumerator<MiningCratePointsUnlockComponent, MiningCrateComponent>();
        while (query.MoveNext(out var uid, out var points, out _))
        {
            if (points.DeniedUntil == TimeSpan.Zero || _timing.CurTime < points.DeniedUntil)
                continue;

            points.DeniedUntil = TimeSpan.Zero;
            Dirty(uid, points);
            SetDenySiren(uid, false);
            if (TryComp<MiningCrateComponent>(uid, out var crate))
                _audio.PlayPvs(crate.UnlockSound, uid);
        }
    }

    private void UpdateUnlockTimers()
    {
        var query = EntityQueryEnumerator<MiningCrateUnlockTimerComponent, MiningCrateComponent, LockComponent>();
        while (query.MoveNext(out var uid, out var timer, out var crate, out var lockComp))
        {
            if (crate.Unlocked || !timer.Started || timer.UnlockAt == TimeSpan.Zero)
                continue;

            if (_timing.CurTime < timer.UnlockAt)
            {
                if (IsDevicePowered(uid) && _timing.CurTime >= timer.NextUnlockBlink)
                {
                    timer.NextUnlockBlink = _timing.CurTime + timer.UnlockBlinkInterval;
                    timer.UnlockBlinkShowUnlocked = !timer.UnlockBlinkShowUnlocked;
                    Dirty(uid, timer);
                    UpdateCrateVisuals(uid);
                }

                continue;
            }

            CompleteUnlock((uid, crate), lockComp);
        }
    }

    private void UpdateTamperBlink()
    {
        var query = EntityQueryEnumerator<MiningCrateComponent>();
        while (query.MoveNext(out var uid, out var crate))
        {
            var ent = (uid, crate);

            TryComp<MiningCrateSecurityComponent>(uid, out var security);
            var detonating = security is { Detonating: true };

            if (!detonating && !IsDevicePowered(uid) && _lights.TryGetLight(uid, out var light) && light.Enabled)
                SetDenySiren(ent, false);

            if (!detonating)
                continue;

            if (_timing.CurTime < crate.NextTamperBlink)
                continue;

            crate.NextTamperBlink = _timing.CurTime + crate.TamperBlinkInterval;
            crate.TamperShowLock = !crate.TamperShowLock;
            Dirty(uid, crate);
            UpdateCrateVisuals(ent);
            if (security is { SirenWireIntact: true })
                _audio.PlayPvs(crate.TamperAlarmSound, uid);
        }
    }

    private void OnActivate(Entity<MiningCrateComponent> ent, ref ActivateInWorldEvent args)
    {
        if (args.Handled || !args.Complex)
            return;

        if (!IsCrateUsable(ent))
        {
            args.Handled = true;
            PowerOffFeedback(ent, args.User);
            return;
        }

        if (ent.Comp.Unlocked)
            return;

        args.Handled = true;
        RequestUnlock(ent, args.User);
    }

    private void OnGetVerbs(Entity<MiningCrateComponent> ent, ref GetVerbsEvent<ActivationVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || ent.Comp.Unlocked)
            return;

        if (!IsCrateUsable(ent))
            return;

        var user = args.User;
        args.Verbs.Add(new ActivationVerb
        {
            Text = Loc.GetString("lavaland-mining-crate-verb-unlock"),
            Act = () => RequestUnlock(ent, user),
        });
    }

    private void OnExamined(Entity<MiningCrateComponent> ent, ref ExaminedEvent args)
    {
        if (TryComp<MiningCrateSecurityComponent>(ent, out var secExam) && secExam.Detonating)
        {
            args.PushMarkup(Loc.GetString("lavaland-mining-crate-examine-tamper"));
            return;
        }

        // Powered-off status is already examined by TogglePower (power-toggle-status-off).
        if (!IsCrateUsable(ent))
            return;

        if (ent.Comp.Unlocked)
        {
            args.PushMarkup(Loc.GetString("lavaland-mining-crate-examine-unlocked"));
            return;
        }

        if (TryComp<MiningCrateUnlockTimerComponent>(ent, out var timer)
            && timer.Started
            && timer.UnlockAt != TimeSpan.Zero)
        {
            var remaining = Math.Max(0, (timer.UnlockAt - _timing.CurTime).TotalSeconds);
            args.PushMarkup(Loc.GetString("lavaland-mining-crate-examine-unlocking", ("seconds", remaining.ToString("0"))));
            return;
        }

        if (TryComp<MiningCratePointsUnlockComponent>(ent, out var points)
            && points.DeniedUntil != TimeSpan.Zero
            && _timing.CurTime < points.DeniedUntil)
        {
            var remaining = Math.Max(0, (points.DeniedUntil - _timing.CurTime).TotalSeconds);
            args.PushMarkup(Loc.GetString("lavaland-mining-crate-examine-denied", ("seconds", remaining.ToString("0"))));
            return;
        }

        if (TryComp<MiningCratePointsUnlockComponent>(ent, out var costPoints))
        {
            args.PushMarkup(Loc.GetString("lavaland-mining-crate-examine-locked", ("cost", costPoints.Cost)));
            return;
        }

        if (HasComp<MiningCrateUnlockTimerComponent>(ent))
        {
            args.PushMarkup(Loc.GetString("lavaland-mining-crate-examine-locked-free"));
            return;
        }

        args.PushMarkup(Loc.GetString("lavaland-mining-crate-locked"));
    }

    private void OnLockToggleAttempt(Entity<MiningCrateComponent> ent, ref LockToggleAttemptEvent args)
    {
        if (!ent.Comp.Unlocked)
            args.Cancelled = true;
    }

    private void OnEmagged(Entity<MiningCrateComponent> ent, ref GotEmaggedEvent args)
    {
        if (!_emag.CompareFlag(args.Type, EmagType.Access))
            return;

        if (ent.Comp.Unlocked)
            return;

        if (TryComp<MiningCrateSecurityComponent>(ent, out var security) && security.Detonating)
            return;

        if (!IsCrateUsable(ent))
        {
            if (_net.IsClient)
            {
                if (_timing.IsFirstTimePredicted)
                    _popup.PopupClient(Loc.GetString("lavaland-mining-crate-powered-off"), ent, args.UserUid);
            }
            else
            {
                _popup.PopupEntity(Loc.GetString("lavaland-mining-crate-powered-off"), ent, args.UserUid);
            }

            return;
        }

        args.Handled = true;
        args.Repeatable = true;

        if (_net.IsClient)
            return;

        if (!TryComp<LockComponent>(ent, out var lockComp))
            return;

        CompleteUnlock(ent, lockComp, args.UserUid);
    }

    private void OnStorageOpenAttempt(Entity<MiningCrateComponent> ent, ref StorageOpenAttemptEvent args)
    {
        if (!IsCrateUsable(ent))
        {
            args.Cancelled = true;
            if (!args.Silent)
                PowerOffFeedback(ent, args.User);
            return;
        }

        if (ent.Comp.Unlocked)
            return;

        args.Cancelled = true;

        if (args.Silent)
            return;

        DenyFeedback(ent, args.User, GetStatusMessage(ent));
    }

    public void RequestUnlock(Entity<MiningCrateComponent> ent, EntityUid user)
    {
        if (ent.Comp.Unlocked)
            return;

        if (TryComp<MiningCrateSecurityComponent>(ent, out var sec) && sec.Detonating)
        {
            DenyFeedback(ent, user, Loc.GetString("lavaland-mining-crate-tamper-active"));
            return;
        }

        if (TryComp<MiningCrateSecurityComponent>(ent, out sec) && sec.LockWireCut)
        {
            DenyFeedback(ent, user, Loc.GetString("lavaland-mining-crate-security-lock-wire-blocked"));
            return;
        }

        if (!IsCrateUsable(ent))
        {
            PowerOffFeedback(ent, user);
            return;
        }

        var ev = new MiningCrateTryUnlockEvent(user);
        RaiseLocalEvent(ent, ref ev);

        if (ev.Handled)
            return;

        DenyFeedback(ent, user, GetStatusMessage(ent));
    }

    private void OnPointsTryUnlock(Entity<MiningCratePointsUnlockComponent> ent, ref MiningCrateTryUnlockEvent args)
    {
        if (args.Handled)
            return;

        if (!TryComp<MiningCrateComponent>(ent, out var crate) || crate.Unlocked)
            return;

        if (!TryComp<MiningCrateUnlockTimerComponent>(ent, out var timer))
        {
            args.Handled = true;
            DenyFeedback(ent, args.User, Loc.GetString("lavaland-mining-crate-locked"));
            return;
        }

        if (timer.Started)
        {
            args.Handled = true;
            DenyFeedback(ent, args.User, GetStatusMessage(ent));
            return;
        }

        if (IsDenyLockedOut(ent.Comp))
        {
            args.Handled = true;
            DenyFeedback(ent, args.User, GetStatusMessage(ent));
            return;
        }

        args.Handled = true;

        if (_net.IsClient)
            return;

        if (_miningPoints.GetPointComp(args.User) is not { } pointsHolder)
        {
            StartDenyLockout(ent, crate, args.User, Loc.GetString("lavaland-mining-crate-no-id"));
            TryShockOnFail(ent, crate, args.User);
            return;
        }

        var available = pointsHolder.Comp?.Points ?? 0;
        if (available < ent.Comp.Cost || !_miningPoints.RemovePoints(pointsHolder, ent.Comp.Cost))
        {
            StartDenyLockout(ent,
                crate,
                args.User,
                Loc.GetString("lavaland-mining-crate-not-enough-points", ("cost", ent.Comp.Cost)));
            TryShockOnFail(ent, crate, args.User);
            return;
        }

        ent.Comp.DeniedUntil = TimeSpan.Zero;
        Dirty(ent);
        StartTimer(ent.Owner, timer, args.User, purchased: true);
    }

    private void OnTimerTryUnlock(Entity<MiningCrateUnlockTimerComponent> ent, ref MiningCrateTryUnlockEvent args)
    {
        if (args.Handled)
            return;

        // Points payment handles TryUnlock itself and calls StartTimer.
        if (HasComp<MiningCratePointsUnlockComponent>(ent))
            return;

        if (!TryComp<MiningCrateComponent>(ent, out var crate) || crate.Unlocked)
            return;

        if (ent.Comp.Started)
        {
            args.Handled = true;
            DenyFeedback(ent, args.User, GetStatusMessage(ent));
            return;
        }

        args.Handled = true;

        if (_net.IsClient)
            return;

        StartTimer(ent, ent.Comp, args.User, purchased: false);
    }

    public void StartTimer(
        EntityUid uid,
        MiningCrateUnlockTimerComponent timer,
        EntityUid user,
        bool purchased)
    {
        if (!TryComp<MiningCrateComponent>(uid, out var crate) || crate.Unlocked)
            return;

        timer.Started = true;
        timer.UnlockAt = _timing.CurTime + timer.UnlockDelay;
        timer.UnlockBlinkShowUnlocked = true;
        timer.NextUnlockBlink = _timing.CurTime + timer.UnlockBlinkInterval;
        Dirty(uid, timer);

        SetDenySiren(uid, false);
        EnsurePhysicallyLocked(uid, user);
        UpdateCrateVisuals(uid);

        _audio.PlayPvs(crate.PurchaseSound, uid);
        _audio.PlayPvs(crate.LockSound, uid);

        var seconds = timer.UnlockDelay.TotalSeconds.ToString("0");
        var msg = purchased
            ? Loc.GetString("lavaland-mining-crate-purchased", ("seconds", seconds))
            : Loc.GetString("lavaland-mining-crate-unlock-started", ("seconds", seconds));
        _popup.PopupEntity(msg, uid, user);
    }

    private void StartDenyLockout(
        Entity<MiningCratePointsUnlockComponent> ent,
        MiningCrateComponent crate,
        EntityUid user,
        string reasonMessage)
    {
        EnsurePhysicallyLocked(ent, user);

        if (ent.Comp.EnableDenyLockout)
        {
            ent.Comp.DeniedUntil = _timing.CurTime + ent.Comp.DenyLockoutDuration;
            Dirty(ent);

            if (IsDevicePowered(ent))
                SetDenySiren(ent, true);

            _audio.PlayPvs(crate.DenySound, ent);
            _audio.PlayPvs(crate.LockSound, ent);

            var remaining = ent.Comp.DenyLockoutDuration.TotalSeconds.ToString("0");
            _popup.PopupEntity(reasonMessage, ent, user);
            _popup.PopupEntity(
                Loc.GetString("lavaland-mining-crate-denied-lockout", ("seconds", remaining)),
                ent,
                user);
            return;
        }

        DenyFeedback(ent, user, reasonMessage);
    }

    private void TryShockOnFail(
        Entity<MiningCratePointsUnlockComponent> ent,
        MiningCrateComponent crate,
        EntityUid user)
    {
        if (ent.Comp.ShockOnFailChance <= 0f)
            return;

        if (!_random.Prob(ent.Comp.ShockOnFailChance))
            return;

        _electrocution.TryDoElectrocution(
            user,
            ent,
            ent.Comp.ShockDamage,
            ent.Comp.ShockDuration,
            refresh: true,
            siemensCoefficient: 1f,
            ignoreInsulation: false);

        _audio.PlayPvs(crate.ShockSound, ent);
        _popup.PopupEntity(Loc.GetString("lavaland-mining-crate-shocked"), ent, user);

        var ev = new MiningCrateForcePowerOffEvent();
        RaiseLocalEvent(ent, ref ev);
        if (!ev.Handled)
            _power.SetPowerDisabled(ent, true);

        SetDenySiren(ent, false);
        UpdateCrateVisuals(ent);
    }

    private bool IsDenyLockedOut(MiningCratePointsUnlockComponent points)
    {
        return points.DeniedUntil != TimeSpan.Zero && _timing.CurTime < points.DeniedUntil;
    }

    public void CompleteUnlock(
        Entity<MiningCrateComponent> ent,
        LockComponent lockComp,
        EntityUid? user = null,
        bool playFeedback = true)
    {
        if (ent.Comp.Unlocked)
            return;

        FillLoot(ent);

        ent.Comp.Unlocked = true;
        Dirty(ent);

        if (TryComp<MiningCrateUnlockTimerComponent>(ent, out var timer))
        {
            timer.Started = false;
            timer.UnlockAt = TimeSpan.Zero;
            timer.UnlockBlinkShowUnlocked = false;
            timer.NextUnlockBlink = TimeSpan.Zero;
            Dirty(ent, timer);
        }

        DisarmSecurity(ent);

        SetDenySiren(ent, false);
        _lock.Unlock(ent, user, lockComp);
        UpdateCrateVisuals(ent);

        if (playFeedback)
        {
            _audio.PlayPvs(ent.Comp.UnlockSound, ent);
            _popup.PopupEntity(Loc.GetString("lavaland-mining-crate-unlocked"), ent);
        }

        var unlockedEv = new MiningCrateUnlockedEvent(user);
        RaiseLocalEvent(ent, ref unlockedEv);
    }

    public void EnsurePhysicallyLocked(EntityUid uid, EntityUid? user = null)
    {
        if (!TryComp<LockComponent>(uid, out var lockComp))
            return;

        if (!lockComp.Locked)
            _lock.Lock(uid, user, lockComp);
    }

    public void DenyFeedback(EntityUid uid, EntityUid user, string message)
    {
        if (!TryComp<MiningCrateComponent>(uid, out var crate))
            return;

        PopupWithSound((uid, crate), user, message, crate.DenySound);
    }

    public void PowerOffFeedback(EntityUid uid, EntityUid user)
    {
        if (!TryComp<MiningCrateComponent>(uid, out var crate))
            return;

        PopupWithSound((uid, crate), user, Loc.GetString("lavaland-mining-crate-powered-off"), crate.PowerOffSound);
    }

    public void SetDenySiren(EntityUid uid, bool enabled)
    {
        if (!TryComp<MiningCrateComponent>(uid, out var crate))
            return;

        SetDenySiren((uid, crate), enabled);
    }

    public void SyncPhysicalSiren(EntityUid uid)
    {
        if (!TryComp<MiningCrateComponent>(uid, out var crate))
            return;

        var wantOn = false;

        if (TryComp<MiningCrateSecurityComponent>(uid, out var security) && security.SirenWireIntact)
        {
            if (security.Detonating)
            {
                wantOn = true;
            }
            else if (crate.EnableDenySiren
                     && TryComp<MiningCratePointsUnlockComponent>(uid, out var points)
                     && points.DeniedUntil != TimeSpan.Zero
                     && _timing.CurTime < points.DeniedUntil
                     && IsDevicePowered(uid))
            {
                wantOn = true;
            }
        }

        ApplySirenLight(uid, wantOn);
    }

    public void UpdateCrateVisuals(EntityUid uid)
    {
        if (!TryComp<MiningCrateComponent>(uid, out var crate))
            return;

        UpdateCrateVisuals((uid, crate));
    }

    public bool IsCrateUsable(EntityUid uid)
    {
        if (!TryComp<MiningCrateComponent>(uid, out var crate))
            return false;

        return IsCrateUsable((uid, crate));
    }

    public bool IsDevicePowered(EntityUid uid)
    {
        SharedApcPowerReceiverComponent? receiver = null;
        if (!_power.ResolveApc(uid, ref receiver))
            return true;

        return _power.IsPowered(uid);
    }

    public string GetStatusMessage(EntityUid uid)
    {
        if (!TryComp<MiningCrateComponent>(uid, out var crate))
            return Loc.GetString("lavaland-mining-crate-locked");

        return GetStatusMessage((uid, crate));
    }

    private void SetDenySiren(Entity<MiningCrateComponent> ent, bool enabled)
    {
        if (enabled && !ent.Comp.EnableDenySiren)
            return;

        if (enabled && TryComp<MiningCrateSecurityComponent>(ent, out var security) && !security.SirenWireIntact)
            return;

        ApplySirenLight(ent.Owner, enabled);
    }

    private void ApplySirenLight(EntityUid uid, bool enabled)
    {
        if (!_lights.TryGetLight(uid, out var light))
            return;

        if (light.Enabled == enabled)
            return;

        _lights.SetEnabled(uid, enabled, light);
    }

    private void UpdateCrateVisuals(Entity<MiningCrateComponent> ent)
    {
        MiningCrateState display;

        if (TryComp<MiningCrateSecurityComponent>(ent, out var secVis) && secVis.Detonating)
        {
            display = ent.Comp.TamperShowLock
                ? MiningCrateState.Locked
                : MiningCrateState.Unlocked;
        }
        else if (!IsDevicePowered(ent))
        {
            display = MiningCrateState.Off;
        }
        else if (TryComp<EntityStorageComponent>(ent, out var storage) && storage.Open)
        {
            display = MiningCrateState.Open;
        }
        else if (ent.Comp.Unlocked)
        {
            display = MiningCrateState.Unlocked;
        }
        else if (TryComp<MiningCrateUnlockTimerComponent>(ent, out var timer)
                 && timer.Started
                 && timer.UnlockAt != TimeSpan.Zero
                 && _timing.CurTime < timer.UnlockAt)
        {
            display = timer.UnlockBlinkShowUnlocked
                ? MiningCrateState.Unlocked
                : MiningCrateState.Locked;
        }
        else
        {
            display = MiningCrateState.Locked;
        }

        _appearance.SetData(ent.Owner, MiningCrateVisuals.Display, display);
    }

    private void DisarmSecurity(EntityUid uid)
    {
        if (!TryComp<MiningCrateSecurityComponent>(uid, out var security))
            return;

        if (!security.Armed || security.Detonating)
            return;

        security.Armed = false;
        Dirty(uid, security);
    }

    private void FillLoot(Entity<MiningCrateComponent> ent)
    {
        if (!ent.Comp.SpawnLoot)
            return;

        if (ent.Comp.LootTable is not { } lootTableId)
        {
            Log.Warning($"Mining crate {ToPrettyString(ent)} has spawnLoot but no lootTable set");
            return;
        }

        if (!_prototypes.TryIndex(lootTableId, out var tableProto))
        {
            Log.Error($"Mining crate {ToPrettyString(ent)} has invalid loot table {lootTableId}");
            return;
        }

        if (!_container.TryGetContainer(ent, SharedEntityStorageSystem.ContainerName, out var container))
        {
            Log.Error($"Mining crate {ToPrettyString(ent)} is missing entity_storage container");
            return;
        }

        var xform = Transform(ent);
        var coords = new EntityCoordinates(ent, Vector2.Zero);

        foreach (var proto in _entityTable.GetSpawns(tableProto))
        {
            var spawn = Spawn(proto, coords);
            if (_container.Insert(spawn, container, containerXform: xform))
                continue;

            _transform.AttachToGridOrMap(spawn);
        }
    }

    private bool IsCrateUsable(Entity<MiningCrateComponent> ent)
    {
        if (!ent.Comp.RequirePower)
            return true;

        return IsDevicePowered(ent);
    }

    private string GetStatusMessage(Entity<MiningCrateComponent> ent)
    {
        if (TryComp<MiningCrateUnlockTimerComponent>(ent, out var timer)
            && timer.Started
            && timer.UnlockAt != TimeSpan.Zero)
        {
            var remaining = Math.Max(0, (timer.UnlockAt - _timing.CurTime).TotalSeconds);
            return Loc.GetString("lavaland-mining-crate-unlocking", ("seconds", remaining.ToString("0")));
        }

        if (TryComp<MiningCratePointsUnlockComponent>(ent, out var points)
            && points.DeniedUntil != TimeSpan.Zero
            && _timing.CurTime < points.DeniedUntil)
        {
            var remaining = Math.Max(0, (points.DeniedUntil - _timing.CurTime).TotalSeconds);
            return Loc.GetString("lavaland-mining-crate-denied-lockout", ("seconds", remaining.ToString("0")));
        }

        if (points != null)
            return Loc.GetString("lavaland-mining-crate-examine-locked", ("cost", points.Cost));

        if (HasComp<MiningCrateUnlockTimerComponent>(ent))
            return Loc.GetString("lavaland-mining-crate-examine-locked-free");

        return Loc.GetString("lavaland-mining-crate-locked");
    }

    private void PowerOffFeedback(Entity<MiningCrateComponent> ent, EntityUid user)
    {
        PopupWithSound(ent, user, Loc.GetString("lavaland-mining-crate-powered-off"), ent.Comp.PowerOffSound);
    }

    private void DenyFeedback(Entity<MiningCrateComponent> ent, EntityUid user, string message)
    {
        PopupWithSound(ent, user, message, ent.Comp.DenySound);
    }

    private void PopupWithSound(
        Entity<MiningCrateComponent> ent,
        EntityUid user,
        string message,
        SoundSpecifier sound)
    {
        if (_net.IsClient)
        {
            if (_timing.IsFirstTimePredicted)
                _popup.PopupClient(message, ent, user);
            return;
        }

        _popup.PopupEntity(message, ent, user);

        if (_timing.CurTime < ent.Comp.NextDenySound)
            return;

        ent.Comp.NextDenySound = _timing.CurTime + ent.Comp.DenySoundDelay;
        Dirty(ent);
        _audio.PlayPvs(sound, ent);
    }
}
