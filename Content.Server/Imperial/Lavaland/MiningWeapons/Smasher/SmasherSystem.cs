using Content.Shared.Imperial.Lavaland.MiningWeapons.Smasher;
using Content.Shared.Imperial.Lavaland.MiningWeapons.Smasher.Components;
using Content.Shared.Imperial.Lavaland.MiningWeapons.Smasher.Events;
using Robust.Shared.Input;
using Robust.Shared.Timing;
using Robust.Shared.Audio.Systems;
using Content.Shared.Alert;
using Content.Shared.CombatMode;
using Content.Shared.FixedPoint;
using Content.Shared.Damage;
using Content.Shared.Movement.Systems;
using Robust.Shared.Utility;

namespace Content.Server.Imperial.Lavaland.MiningWeapons.Smasher;

public sealed class SmasherSystem : SharedSmasherSystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedCombatModeSystem _combatMode = default!;
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeed = default!;

    // Used dictionaries because system dictionaries store temporary server-side state;
    // components store permanent networked data.
    private Dictionary<EntityUid, ChargeData> _activeCharges = new();
    private Dictionary<EntityUid, EntityUid> _lastAlertedUser = new();
    private Dictionary<EntityUid, FixedPoint2> _lastTotalDamage = new();
    private List<EntityUid> _usersWithDecay = new();
    private TimeSpan _decayEndTime;
    private bool _isDecayEffectActive;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<UpdateSmasherStateEvent>(OnUpdateKeyState);

        SubscribeLocalEvent<ShieldActiveComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<ShieldActiveComponent, ComponentShutdown>(OnShieldShutdown);
        SubscribeLocalEvent<SmasherChargingComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshMovespeed);
        SubscribeLocalEvent<ShieldActiveComponent, DamageModifyEvent>(OnUserDamageModified);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_isDecayEffectActive && _timing.CurTime >= _decayEndTime)
        {
            foreach (var user in _usersWithDecay)
            {
                HideShieldEffect(user);
            }
            _usersWithDecay.Clear();
            _isDecayEffectActive = false;
        }

        var smasherQuery = EntityQueryEnumerator<SmasherComponent>();
        while (smasherQuery.MoveNext(out var smasherUid, out var smasher))
        {
            // Alert update
            if (!TryGetHolder(smasherUid, out var user))
            {
                if (_activeCharges.TryGetValue(smasherUid, out var chargeData))
                {
                    _alerts.ClearAlert(chargeData.User, smasher.CounterCooldownAlert);
                }

                if (_lastAlertedUser.TryGetValue(smasherUid, out var lastUser))
                {
                    _alerts.ClearAlert(lastUser, smasher.CounterCooldownAlert);
                }
                continue;
            }

            _lastAlertedUser[smasherUid] = user.Value;

            UpdateCooldownAlert(smasherUid, user.Value, smasher);

            if (!IsItemWielded(smasherUid))
                continue;

            // Shield logic
            if (smasher.StateUseKey == BoundKeyState.Down &&
                !HasComp<ShieldActiveComponent>(user) &&
                _combatMode.IsInCombatMode(user.Value) &&
                CanActivateShield(smasher))
            {
                ProcessCharging(user.Value, smasherUid, smasher);
            }
            else if (_activeCharges.ContainsKey(smasherUid))
            {
                CancelCharging(user.Value, smasherUid, smasher);
            }
        }

        // termination of active shield after some time
        var shieldQuery = EntityQueryEnumerator<ShieldActiveComponent>();
        while (shieldQuery.MoveNext(out var uid, out var shield))
        {
            if (_timing.CurTime >= shield.EndTime)
            {
                DeactivateShield(uid, shield);
            }
        }
    }


    private void OnUpdateKeyState(UpdateSmasherStateEvent ev, EntitySessionEventArgs args)
    {
        var user = GetEntity(ev.User);
        if (!TryGetSmasherInHands(user, out var smasherUid, out var smasher))
            return;

        smasher.StateUseKey = ev.KeyState;
        Dirty(smasherUid.Value, smasher);
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

    private void OnShieldShutdown(EntityUid user, ShieldActiveComponent component, ComponentShutdown args)
    {
        if (component.EffectDecay != null && component.TimeDecay != null)
        {
            ShowShieldEffect(user, component.EffectDecay, false);
            _isDecayEffectActive = true;
            _decayEndTime = _timing.CurTime + component.TimeDecay.Value;
            _usersWithDecay.Add(user);
        }
        else
        {
            HideShieldEffect(user);
        }

        if (TryGetSmasherInHands(user, out var _, out var smasher))
            _audio.PlayPvs(smasher.DeactivateSound, user);
    }

    private void OnRefreshMovespeed(EntityUid uid, SmasherChargingComponent component, RefreshMovementSpeedModifiersEvent args)
    {
        args.ModifySpeed(component.WalkSpeedModifier, component.SprintSpeedModifier);
    }


    private void ProcessCharging(EntityUid user, EntityUid smasherUid, SmasherComponent smasher)
    {
        if (!_activeCharges.TryGetValue(smasherUid, out var chargeData))
        {
            chargeData = new ChargeData
            {
                StartTime = _timing.CurTime,
                User = user
            };
            _activeCharges[smasherUid] = chargeData;

            ShowShieldEffect(user, smasher.EffectCharging, true);

            EnsureComp<SmasherChargingComponent>(user);
            _movementSpeed.RefreshMovementSpeedModifiers(user);

            _audio.PlayPvs(smasher.StartChargingSound, user);
        }

        var holdTime = (_timing.CurTime - chargeData.StartTime).TotalSeconds;

        if (CheckDamageInterruption(user))
        {
            CancelCharging(user, smasherUid, smasher);
            return;
        }

        if (holdTime >= smasher.TimeChargingSmasher)
        {
            CompleteCharging(user, smasherUid, smasher);
        }
    }

    private void CancelCharging(EntityUid user, EntityUid smasherUid, SmasherComponent smasher)
    {
        _activeCharges.Remove(smasherUid);

        if (smasher.EffectDecay != null)
        {
            ShowShieldEffect(user, smasher.EffectDecay, false);
            _isDecayEffectActive = true;
            _decayEndTime = _timing.CurTime + smasher.TimeDecay;
            _usersWithDecay.Add(user);
        }

        if (HasComp<SmasherChargingComponent>(user))
        {
            RemComp<SmasherChargingComponent>(user);
            _movementSpeed.RefreshMovementSpeedModifiers(user);
        }

        _audio.PlayPvs(smasher.DeactivateSound, user);

        SetCooldown(smasherUid, smasher, smasher.TimeCooldownDownedDecay);
    }

    private void CompleteCharging(EntityUid user, EntityUid smasherUid, SmasherComponent smasher)
    {
        _activeCharges.Remove(smasherUid);

        ActivateShield(smasherUid, smasher, user);

        HideShieldEffect(user);
        ShowShieldEffect(user, smasher.EffectActived, true);

        if (HasComp<SmasherChargingComponent>(user))
        {
            RemComp<SmasherChargingComponent>(user);
            _movementSpeed.RefreshMovementSpeedModifiers(user);
        }
    }

    public void ActivateShield(EntityUid smasherUid, SmasherComponent smasher, EntityUid user)
    {
        if (HasComp<SmasherChargingComponent>(user))
        {
            RemComp<SmasherChargingComponent>(user);
            _movementSpeed.RefreshMovementSpeedModifiers(user);
        }

        var shieldActive = AddComp<ShieldActiveComponent>(user);
        shieldActive.TimeDecay = smasher.TimeDecay;
        shieldActive.EffectActived = smasher.EffectActived;
        shieldActive.SmasherUid = smasherUid;
        shieldActive.EndTime = _timing.CurTime + smasher.ActiveShieldTime;
        Dirty(user, shieldActive);

        SetCooldown(smasherUid, smasher, smasher.ActiveShieldCooldown);

        ShieldActivated(user, smasher);
    }

    private void ShieldActivated(EntityUid user, SmasherComponent smasher)
    {
        HideShieldEffect(user);
        ShowShieldEffect(user, smasher.EffectActived, true);

        _audio.PlayPvs(smasher.ActivateSound, user);
    }

    private void DeactivateShield(EntityUid user, ShieldActiveComponent shield)
    {
        if (shield.EffectDecay != null && TryComp<SmasherComponent>(shield.SmasherUid, out var smasher))
        {
            ShowShieldEffect(user, smasher.EffectDecay, false);
            _isDecayEffectActive = true;
            _decayEndTime = _timing.CurTime + smasher.TimeDecay;
            _usersWithDecay.Add(user);
        }

        RemComp<ShieldActiveComponent>(user);
    }

    private void ShowShieldEffect(EntityUid user, SpriteSpecifier? effect, bool loop)
    {
        if (effect == null) return;
        RaiseNetworkEvent(new ShowShieldEffectEvent(GetNetEntity(user), effect, loop));
    }

    private void HideShieldEffect(EntityUid user)
    {
        RaiseNetworkEvent(new HideShieldEffectEvent(GetNetEntity(user)));
    }

    private bool CheckDamageInterruption(EntityUid user)
    {
        if (!TryComp<DamageableComponent>(user, out var damageComp))
            return false;

        if (!_lastTotalDamage.TryGetValue(user, out var lastDamage))
        {
            _lastTotalDamage[user] = damageComp.TotalDamage;
            return false;
        }

        var damageReceived = damageComp.TotalDamage - lastDamage;

        if (damageReceived > FixedPoint2.New(1.0))
        {
            HideShieldEffect(user);

            if (TryGetSmasherInHands(user, out var _, out var smasher) &&
                smasher.EffectDecay != null)
            {
                ShowShieldEffect(user, smasher.EffectDecay, false);
                _isDecayEffectActive = true;
                _decayEndTime = _timing.CurTime + TimeSpan.FromSeconds(1.8);
                _usersWithDecay.Add(user);
            }

            if (HasComp<SmasherChargingComponent>(user))
            {
                RemComp<SmasherChargingComponent>(user);
                _movementSpeed.RefreshMovementSpeedModifiers(user);
            }

            _lastTotalDamage.Remove(user);
            return true;
        }

        _lastTotalDamage[user] = damageComp.TotalDamage;
        return false;
    }
}
