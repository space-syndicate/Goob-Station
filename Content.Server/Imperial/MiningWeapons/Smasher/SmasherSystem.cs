using Content.Shared.Imperial.MiningWeapons.Smasher;
using Content.Shared.Imperial.MiningWeapons.Smasher.Components;
using Content.Shared.Imperial.MiningWeapons.Smasher.Events;
using Content.Shared.Imperial.MiningWeapons;
using Robust.Shared.Input;
using Robust.Shared.Timing;
using Robust.Shared.Audio.Systems;
using Content.Shared.Alert;
using Content.Shared.CombatMode;
using Content.Shared.Damage;
using Content.Shared.Movement.Systems;

namespace Content.Server.Imperial.MiningWeapons.Smasher;

public sealed partial class SmasherSystem : SharedSmasherSystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedCombatModeSystem _combatMode = default!;
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeed = default!;
    [Dependency] private readonly MiningWeaponsHelpers _miningWeaponsHelpers = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<UpdateSmasherStateEvent>(OnUpdateKeyState);
        SubscribeLocalEvent<ShieldActiveComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<ShieldActiveComponent, DamageModifyEvent>(OnUserDamageModified);
        SubscribeLocalEvent<SmasherChargingComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshMovespeed);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        UpdateSmashers();
        UpdateActiveShields();
    }

    #region Update Methods
    private void UpdateSmashers()
    {
        var smasherQuery = EntityQueryEnumerator<SmasherComponent>();
        while (smasherQuery.MoveNext(out var smasherUid, out var smasher))
        {
            ProcessSmasher(smasherUid, smasher);
        }
    }

    private void UpdateActiveShields()
    {
        var shieldQuery = EntityQueryEnumerator<ShieldActiveComponent>();
        while (shieldQuery.MoveNext(out var uid, out var shield))
        {
            if (_timing.CurTime >= shield.EndTime)
            {
                DeactivateShield(uid, shield);
            }
        }
    }

    private void ProcessSmasher(EntityUid smasherUid, SmasherComponent smasher)
    {
        if (!TryGetHolder(smasherUid, out var user))
        {
            CleanupAlerts(smasherUid, smasher);
            return;
        }

        smasher.LastAlertedUser[smasherUid] = user.Value;
        UpdateCooldownAlert(smasherUid, user.Value, smasher);

        if (!_miningWeaponsHelpers.IsItemWielded(smasherUid))
            return;

        if (smasher.StateUseKey == BoundKeyState.Down &&
            !HasComp<ShieldActiveComponent>(user) &&
            _combatMode.IsInCombatMode(user.Value) &&
            CanActivateShield(smasher))
        {
            ProcessCharging(user.Value, smasherUid, smasher);
        }
        else if (smasher.ActiveCharges.ContainsKey(smasherUid))
        {
            CancelCharging(user.Value, smasherUid, smasher);
        }
    }

    private void ProcessCharging(EntityUid user, EntityUid smasherUid, SmasherComponent smasher)
    {
        if (!smasher.ActiveCharges.TryGetValue(smasherUid, out var chargeData))
        {
            StartCharging(user, smasherUid, smasher);
            return;
        }

        var holdTime = _timing.CurTime - chargeData.StartTime;

        if (CheckDamageInterruption(user, smasher))
        {
            CancelCharging(user, smasherUid, smasher);
            return;
        }

        if (holdTime >= smasher.TimeChargingSmasher)
        {
            CompleteCharging(user, smasherUid, smasher);
        }
    }
    #endregion

    #region Event Handlers
    private void OnUpdateKeyState(UpdateSmasherStateEvent ev, EntitySessionEventArgs args)
    {
        var user = GetEntity(ev.User);
        if (!TryGetSmasherInHands(user, out var _, out var smasher))
            return;

        smasher.StateUseKey = ev.KeyState;
    }

    private void OnUserDamageModified(EntityUid uid, ShieldActiveComponent component, ref DamageModifyEvent args)
    {
        var modifier = component.PassiveBlockDamageModifer;
        if (modifier == null)
            return;

        args.Damage = DamageSpecifier.ApplyModifierSet(args.Damage, modifier);
    }

    private void OnComponentInit(EntityUid uid, ShieldActiveComponent component, ComponentInit args)
    {
        if (!TryGetSmasherInHands(uid, out var _, out var smasher))
            return;

        component.PassiveBlockDamageModifer ??= new DamageModifierSet();
        component.PassiveBlockDamageModifer.Coefficients ??= new Dictionary<string, float>();
        component.PassiveBlockDamageModifer.Coefficients = smasher.DamageBlockedCoefficients;
    }

    private void OnRefreshMovespeed(EntityUid uid, SmasherChargingComponent component, RefreshMovementSpeedModifiersEvent args)
    {
        args.ModifySpeed(component.WalkSpeedModifier, component.SprintSpeedModifier);
    }
    #endregion
}
