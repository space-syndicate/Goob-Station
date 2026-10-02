// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Electrocution;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Shared.Buckle.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.DeviceLinking;
using Content.Shared.DeviceLinking.Events;
using Content.Shared.Electrocution;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Power;
using Content.Shared.Power.Components;
using Content.Shared.Power.EntitySystems;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Content.Server._CorvaxGoob.ExecutionChair;

public sealed class ExecutionChairSystem : EntitySystem
{
    private static readonly ProtoId<SinkPortPrototype> TogglePort = "Toggle";
    private static readonly ProtoId<SinkPortPrototype> OnPort = "On";
    private static readonly ProtoId<SinkPortPrototype> OffPort = "Off";
    private static readonly ProtoId<SpeciesPrototype> IpcSpecies = "IPC";
    private static readonly ProtoId<DamageTypePrototype> ShockDamage = "Shock";

    [Dependency] private readonly ElectrocutionSystem _electrocution = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedPowerReceiverSystem _powerReceiver = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private ISawmill _sawmill = default!;

    public override void Initialize()
    {
        base.Initialize();

        UpdatesAfter.Add(typeof(PowerNetSystem));

        SubscribeLocalEvent<ExecutionChairComponent, MapInitEvent>(OnMapInitEvent);
        SubscribeLocalEvent<ExecutionChairComponent, ComponentShutdown>(OnExecutionChairShutdownEvent);
        SubscribeLocalEvent<ExecutionChairComponent, SignalReceivedEvent>(OnSignalReceivedEvent);
        SubscribeLocalEvent<ExecutionChairComponent, StrapAttemptEvent>(OnStrapAttemptEvent);
        SubscribeLocalEvent<ExecutionChairComponent, StrappedEvent>(OnStrappedEvent);
        SubscribeLocalEvent<ExecutionChairComponent, UnstrappedEvent>(OnUnstrappedEvent);
        SubscribeLocalEvent<ExecutionChairComponent, PowerChangedEvent>(OnPowerChangedEvent);
        SubscribeLocalEvent<ExecutionChairComponent, AnchorStateChangedEvent>(OnAnchorStateChangedEvent);
        SubscribeLocalEvent<HumanoidAppearanceComponent, DamageModifyEvent>(OnDamageModifyEvent);

        EntityManager.ComponentRemoved += OnComponentRemoved;
        _sawmill = Logger.GetSawmill("execution_chair");
    }

    public override void Shutdown()
    {
        base.Shutdown();

        EntityManager.ComponentRemoved -= OnComponentRemoved;
    }

    private void OnMapInitEvent(Entity<ExecutionChairComponent> ent, ref MapInitEvent args)
    {
        TurnOff(ent.Owner, false);

        if (!TryGetChairComponents(ent.Owner, out _, out _, out _, out _))
            LogInvalidComposition(ent.Owner);
    }

    private void OnExecutionChairShutdownEvent(Entity<ExecutionChairComponent> ent, ref ComponentShutdown args)
    {
        if (TerminatingOrDeleted(ent.Owner))
            return;

        TurnOff(ent.Owner, false);
    }

    private void OnComponentRemoved(RemovedComponentEventArgs args)
    {
        var uid = args.BaseArgs.Owner;
        if (!IsLiveExecutionChair(uid))
            return;

        switch (args.BaseArgs.Component)
        {
            case ApcPowerReceiverComponent:
                var wasArmed = TryComp<ElectrifiedComponent>(uid, out var electrified) && electrified.Enabled;
                SetElectrifiedEnabled(uid, false, electrified);
                RemoveRuntimeComponents(uid);

                if (wasArmed)
                    ShowTurnOffPopup(uid);
                break;
            case ElectrifiedComponent removedElectrified:
                _powerReceiver.SetPowerDisabled(uid, true);
                RemoveRuntimeComponents(uid);

                if (removedElectrified.Enabled)
                    ShowTurnOffPopup(uid);
                break;
            case StrapComponent:
            case DeviceLinkSinkComponent:
            case ExtensionCableReceiverComponent:
                TurnOff(uid, true);
                break;
            default:
                return;
        }

        LogInvalidComposition(uid);
    }

    private void OnSignalReceivedEvent(Entity<ExecutionChairComponent> ent, ref SignalReceivedEvent args)
    {
        var pending = HasComp<ExecutionChairPowerPendingComponent>(ent.Owner);
        var armed = TryComp<ElectrifiedComponent>(ent.Owner, out var electrified) && electrified.Enabled;

        if (args.Port == TogglePort)
        {
            if (pending || armed)
                TurnOff(ent.Owner, true);
            else
                BeginPowerUp(ent.Owner);

            return;
        }

        if (args.Port == OnPort)
        {
            if (!pending && !armed)
                BeginPowerUp(ent.Owner);

            return;
        }

        if (args.Port == OffPort)
        {
            TurnOff(ent.Owner, true);
            return;
        }

        _sawmill.Debug($"Received unexpected port signal: {args.Port} on chair {ToPrettyString(ent.Owner)}");
    }

    private void OnStrapAttemptEvent(Entity<ExecutionChairComponent> ent, ref StrapAttemptEvent args)
    {
        if (args.Strap.Comp.BuckledEntities.Count != 0)
            args.Cancelled = true;
    }

    private void OnStrappedEvent(Entity<ExecutionChairComponent> ent, ref StrappedEvent args)
    {
        if (TryComp<ElectrifiedComponent>(ent.Owner, out var electrified))
            UpdateActivity(ent.Owner, args.Strap.Comp, electrified);
    }

    private void OnUnstrappedEvent(Entity<ExecutionChairComponent> ent, ref UnstrappedEvent args)
    {
        if (TryComp<ElectrifiedComponent>(ent.Owner, out var electrified))
            UpdateActivity(ent.Owner, args.Strap.Comp, electrified);
    }

    private void OnPowerChangedEvent(Entity<ExecutionChairComponent> ent, ref PowerChangedEvent args)
    {
        if (!TryComp<ElectrifiedComponent>(ent.Owner, out var electrified))
            return;

        if (!args.Powered)
        {
            if (electrified.Enabled)
                TurnOff(ent.Owner, true);

            return;
        }

        if (electrified.Enabled && TryComp<StrapComponent>(ent.Owner, out var strap))
            UpdateActivity(ent.Owner, strap, electrified);
    }

    private void OnAnchorStateChangedEvent(Entity<ExecutionChairComponent> ent, ref AnchorStateChangedEvent args)
    {
        if (args.Anchored)
            return;

        TurnOff(ent.Owner, true);
    }

    private void OnDamageModifyEvent(Entity<HumanoidAppearanceComponent> ent, ref DamageModifyEvent args)
    {
        if (ent.Comp.Species != IpcSpecies ||
            !TryComp<MobStateComponent>(ent.Owner, out var mobState) ||
            _mobState.IsDead(ent.Owner, mobState) ||
            !HasComp<BatteryComponent>(ent.Owner) ||
            args.Origin is not { } origin ||
            !HasComp<ExecutionChairComponent>(origin) ||
            !TryComp<StrapComponent>(origin, out var strap) ||
            !IsBuckledTo(ent.Owner, strap))
        {
            return;
        }

        var damage = args.Damage.DamageDict;
        if (damage.Count != 1 ||
            !damage.TryGetValue(ShockDamage, out var shock) ||
            shock <= 0)
        {
            return;
        }

        args.Damage = new DamageSpecifier();
    }

    private static bool IsBuckledTo(EntityUid target, StrapComponent strap)
    {
        foreach (var buckled in strap.BuckledEntities)
        {
            if (buckled == target)
                return true;
        }

        return false;
    }

    public override void Update(float frameTime)
    {
        UpdatePendingChairs();
        UpdateActiveChairs();
    }

    private void UpdatePendingChairs()
    {
        var query = EntityQueryEnumerator<ExecutionChairPowerPendingComponent, ExecutionChairComponent>();
        while (query.MoveNext(out var uid, out var pending, out _))
        {
            if (_timing.CurTime < pending.CheckAt)
                continue;

            if (!TryGetChairComponents(uid, out var receiver, out var strap, out var electrified, out var transform))
            {
                LogInvalidComposition(uid);
                FailPowerUp(uid);
                continue;
            }

            if (receiver.PowerDisabled || !receiver.Powered || !transform.Anchored)
            {
                FailPowerUp(uid);
                continue;
            }

            RemCompDeferred<ExecutionChairPowerPendingComponent>(uid);
            SetElectrifiedEnabled(uid, true, electrified);
            _popup.PopupEntity(Loc.GetString("execution-chair-turn-on"), uid, PopupType.Medium);
            UpdateActivity(uid, strap, electrified);
        }
    }

    private void UpdateActiveChairs()
    {
        var query = EntityQueryEnumerator<ActiveExecutionChairComponent, ExecutionChairComponent>();
        while (query.MoveNext(out var uid, out var active, out _))
        {
            if (!TryGetChairComponents(uid, out var receiver, out var strap, out var electrified, out var transform))
            {
                LogInvalidComposition(uid);
                TurnOff(uid, true);
                continue;
            }

            if (receiver.PowerDisabled || !receiver.Powered || !electrified.Enabled || !transform.Anchored)
            {
                TurnOff(uid, true);
                continue;
            }

            if (strap.BuckledEntities.Count == 0)
            {
                RemCompDeferred<ActiveExecutionChairComponent>(uid);
                continue;
            }

            if (strap.BuckledEntities.Count > 1)
            {
                _sawmill.Error(
                    $"Execution chair {ToPrettyString(uid)} has {strap.BuckledEntities.Count} occupants.");
                TurnOff(uid, true);
                continue;
            }

            if (_timing.CurTime < active.NextShockTime)
                continue;

            active.NextShockTime += GetShockCooldown(electrified);

            var target = strap.BuckledEntities.First();
            _electrocution.TryDoElectrifiedAct(uid, target, electrified: electrified, transform: transform);
        }
    }

    private void BeginPowerUp(EntityUid uid)
    {
        if (!TryGetChairComponents(uid, out var receiver, out _, out var electrified, out var transform))
        {
            LogInvalidComposition(uid);
            FailPowerUp(uid);
            return;
        }

        if (!transform.Anchored)
        {
            FailPowerUp(uid);
            return;
        }

        SetElectrifiedEnabled(uid, false, electrified);
        RemComp<ActiveExecutionChairComponent>(uid);
        _powerReceiver.SetPowerDisabled(uid, false, receiver);

        var pending = EnsureComp<ExecutionChairPowerPendingComponent>(uid);
        pending.CheckAt = _timing.CurTime + _timing.TickPeriod;
    }

    private void FailPowerUp(EntityUid uid)
    {
        TurnOff(uid, false);
        _popup.PopupEntity(Loc.GetString("execution-chair-power-failed"), uid, PopupType.Medium);
    }

    private void TurnOff(EntityUid uid, bool showPopup)
    {
        var wasArmed = TryComp<ElectrifiedComponent>(uid, out var electrified) && electrified.Enabled;

        _powerReceiver.SetPowerDisabled(uid, true);

        SetElectrifiedEnabled(uid, false, electrified);
        RemoveRuntimeComponents(uid);

        if (showPopup && wasArmed)
            ShowTurnOffPopup(uid);
    }

    private void UpdateActivity(EntityUid uid, StrapComponent strap, ElectrifiedComponent electrified)
    {
        if (!electrified.Enabled || strap.BuckledEntities.Count == 0)
        {
            RemComp<ActiveExecutionChairComponent>(uid);
            return;
        }

        if (HasComp<ActiveExecutionChairComponent>(uid))
            return;

        var active = AddComp<ActiveExecutionChairComponent>(uid);
        active.NextShockTime = _timing.CurTime + GetShockCooldown(electrified);
    }

    private TimeSpan GetShockCooldown(ElectrifiedComponent electrified)
    {
        return electrified.ShockCooldown > TimeSpan.Zero
            ? electrified.ShockCooldown
            : _timing.TickPeriod;
    }

    private bool TryGetChairComponents(
        EntityUid uid,
        [NotNullWhen(true)] out ApcPowerReceiverComponent? receiver,
        [NotNullWhen(true)] out StrapComponent? strap,
        [NotNullWhen(true)] out ElectrifiedComponent? electrified,
        [NotNullWhen(true)] out TransformComponent? transform)
    {
        receiver = null;
        strap = null;
        electrified = null;
        transform = null;

        if (!Resolve(uid, ref receiver, ref strap, ref electrified, ref transform, false))
        {
            return false;
        }

        return receiver.Load > 0 &&
               HasComp<DeviceLinkSinkComponent>(uid) &&
               HasComp<ExtensionCableReceiverComponent>(uid);
    }

    private bool IsLiveExecutionChair(EntityUid uid)
    {
        return !TerminatingOrDeleted(uid) && HasComp<ExecutionChairComponent>(uid);
    }

    private void RemoveRuntimeComponents(EntityUid uid)
    {
        RemComp<ExecutionChairPowerPendingComponent>(uid);
        RemComp<ActiveExecutionChairComponent>(uid);
    }

    private void ShowTurnOffPopup(EntityUid uid)
    {
        _popup.PopupEntity(
            Loc.GetString("execution-chair-turn-off"),
            uid,
            PopupType.Medium);
    }

    private void SetElectrifiedEnabled(
        EntityUid uid,
        bool enabled,
        ElectrifiedComponent? electrified = null)
    {
        if (!Resolve(uid, ref electrified, false))
            return;

        _electrocution.SetElectrified((uid, electrified), enabled);
    }

    private void LogInvalidComposition(EntityUid uid)
    {
        _sawmill.Warning($"Execution chair {ToPrettyString(uid)} has an invalid component composition.");
    }
}
