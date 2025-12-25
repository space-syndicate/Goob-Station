using Content.Shared.Imperial.Lavaland.MiningWeapons.Smasher;
using Content.Shared.Imperial.Lavaland.MiningWeapons.Smasher.Events;
using Content.Shared.Imperial.Lavaland.MiningWeapons.Smasher.Components;
using Content.Shared.Imperial.Lavaland.MiningWeapons.Smasher.Enums;
using Content.Shared.CombatMode;
using Content.Shared.FixedPoint;
using Content.Shared.Alert;
using Content.Shared.Damage;
using Robust.Shared.Prototypes;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Timing;
using Robust.Shared.Input;
using Robust.Shared.Utility;
using Robust.Client.GameObjects;
using Robust.Client.Player;

namespace Content.Client.Imperial.Lavaland.MiningWeapons.Smasher;

public sealed class SmasherSystem : SharedSmasherSystem
{
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly InputSystem _inputSystem = default!;
    [Dependency] private readonly SharedCombatModeSystem _combatMode = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;


    private Dictionary<EntityUid, FixedPoint2> _lastTotalDamage = new();
    private ProtoId<AlertPrototype> _counterCooldownAlert = "SmasherCounterCooldown";
    private TimeSpan _timeDecay = TimeSpan.FromSeconds(1.8f); // There are 6 states in total, each lasting 0.3 seconds.
    private TimeSpan _timeCooldownDownedDecay = TimeSpan.FromSeconds(5.0f); // Includes shield decay time
    private TimeSpan _timeCooldownCompleted = TimeSpan.FromSeconds(10f);
    private TimeSpan _holdStartTime;
    private TimeSpan _cooldownEnd;
    private TimeSpan _decayEndTime;
    private float _timeChargingSmasher = 4.0f;
    private bool _isHolding; // Tracking the right mouse button hold to charge the shield.
    private bool _isChargingEffectActive;
    private bool _isDecayEffectActive;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<ShieldActivatedEvent>(OnShieldActivated);
        SubscribeNetworkEvent<ShieldChargingEvent>(OnShieldCharging);
        SubscribeNetworkEvent<ShieldDecayEvent>(OnShieldDecay);
        SubscribeNetworkEvent<ShowShieldEffectEvent>(OnShowShieldEffect);
        SubscribeNetworkEvent<HideShieldEffectEvent>(OnHideShieldEffect);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var user = _player.LocalEntity;
        if (user == null) return;

        // Check if decay effect time has expired
        if (_isDecayEffectActive && _timing.CurTime >= _decayEndTime)
        {
            if (user.HasValue)
            {
                HideShieldEffect(user.Value);
                _isDecayEffectActive = false;
            }
        }

        if (_timing.CurTime >= _cooldownEnd)
        {
            _cooldownEnd = TimeSpan.Zero;
        }

        // Check if Smasher weapon is held in hands
        if (!TryGetSmasherInHands(user.Value, out var smasherUid, out var smasher))
        {
            // If weapon is not held but charging was active - cancel it
            if (_isHolding)
            {
                if (_isChargingEffectActive)
                {
                    HideShieldEffect(user.Value);
                    _isChargingEffectActive = false;
                }

                RaiseNetworkEvent(new ShieldChargingEndEvent(GetNetEntity(user.Value)));

                _isHolding = false;
            }
            return;
        }

        UpdateCooldownAlert(user.Value);

        var useKey = EngineKeyFunctions.UseSecondary;

        if (_inputSystem.CmdStates.GetState(useKey) == BoundKeyState.Down && !HasComp<ShieldActiveComponent>(user))
        {
            // Start shield charging process
            if (!_isHolding)
            {
                if (!_combatMode.IsInCombatMode(user))
                    return;

                if (_timing.CurTime < _cooldownEnd)
                    return;

                _holdStartTime = _timing.CurTime;
                _isHolding = true;

                if (smasher.EffectCharging != null)
                {
                    _isChargingEffectActive = true;

                    ShowShieldEffect(user.Value, smasher.EffectCharging, true);
                    RaiseNetworkEvent(new ShieldChargingEvent(GetNetEntity(user.Value), smasher.EffectCharging));
                }

                RaiseNetworkEvent(new ShieldChargingEvent(GetNetEntity(user.Value), smasher.EffectCharging));

                Log.Info("Начата зарядка щита");
            }

            var holdTime = (_timing.CurTime - _holdStartTime).TotalSeconds;
            Log.Debug($"Зарядка: {holdTime:F1} сек");

            // To prevent the destruction of shield of smasher with minimal exposure to external factors (e.g. atmosphere)
            if (_isHolding && _isChargingEffectActive)
                CheckDamageInterruption(user.Value);

            if (holdTime >= _timeChargingSmasher)
            {
                Log.Debug("Зарядка завершена - активируем щит");
                RaiseNetworkEvent(new ShieldActivatedEvent(GetNetEntity(smasherUid.Value), NetEntity.Invalid,
                    smasher.EffectActived, smasher.EffectCharging, smasher.EffectDecay));

                RaiseNetworkEvent(new ShieldChargingEndEvent(GetNetEntity(user.Value)));

                _isHolding = false;
                _cooldownEnd = _timing.CurTime + _timeCooldownCompleted;
            }
        }
        // Charging cancellation (button released)
        else if (_isHolding)
        {
            if (_isChargingEffectActive)
            {
                HideShieldEffect(user.Value);
                _isChargingEffectActive = false;

                if (smasher.EffectDecay != null)
                {
                    ShowShieldEffect(user.Value, smasher.EffectDecay, true);
                    _isDecayEffectActive = true;
                    _decayEndTime = _timing.CurTime + _timeDecay;
                    _cooldownEnd = _timing.CurTime + _timeCooldownDownedDecay;
                }
            }

            RaiseNetworkEvent(new ShieldChargingEndEvent(GetNetEntity(user.Value)));

            _isHolding = false;
        }
    }

    private void OnShowShieldEffect(ShowShieldEffectEvent ev)
    {
        ShowShieldEffect(GetEntity(ev.Uid), ev.EffectDecay, true);
    }

    private void OnHideShieldEffect(HideShieldEffectEvent ev)
    {
        HideShieldEffect(GetEntity(ev.Uid));
    }

    private void OnShieldCharging(ShieldChargingEvent ev)
    {
        if (!TryGetEntity(ev.User, out var userUid))
            return;

        if (ev.EffectCharging != null)
        {
            ShowShieldEffect(userUid.Value, ev.EffectCharging, true);
        }
    }

    private void OnShieldDecay(ShieldDecayEvent ev)
    {
        if (!TryGetEntity(ev.User, out var userUid))
            return;

        if (ev.EffectDecay != null)
        {
            ShowShieldEffect(userUid.Value, ev.EffectDecay, true);
            _isDecayEffectActive = true;
            _decayEndTime = _timing.CurTime + _timeDecay;
        }
    }

    private void OnShieldActivated(ShieldActivatedEvent ev)
    {
        _isHolding = false;
        _isChargingEffectActive = false;

        if (!TryGetEntity(ev.User, out var userUid))
            return;

        HideShieldEffect(userUid.Value);

        if (TryComp<ShieldActiveComponent>(userUid.Value, out var shieldActive))
            _audio.PlayPvs(shieldActive.ActivateSound, userUid.Value);

        if (ev.EffectActived != null)
        {
            ShowShieldEffect(userUid.Value, ev.EffectActived, true);
        }
    }

    /// <summary>
    /// Updates the cooldown alert display based on remaining cooldown time
    /// </summary>
    private void UpdateCooldownAlert(EntityUid user)
    {
        if (!user.Valid)
        {
            _alerts.ClearAlert(user, _counterCooldownAlert);
            return;
        }

        var remainingCooldown = _cooldownEnd - _timing.CurTime;
        var secondsRemaining = (int)Math.Ceiling(remainingCooldown.TotalSeconds);

        // If the cooldown is negative (ended), show 0
        if (secondsRemaining <= 0)
            secondsRemaining = 0;

        var alertSeverity = CalculateAlertSeverity(secondsRemaining);
        _alerts.ShowAlert(user, _counterCooldownAlert, (short)alertSeverity);
    }


    /// <summary>
    /// Calculates alert severity based on remaining cooldown seconds
    /// Rounds up to nearest multiple of 5
    /// </summary>
    private int CalculateAlertSeverity(int secondsRemaining)
    {
        var roundedSeconds = (int)Math.Ceiling(secondsRemaining / 5.0) * 5;
        roundedSeconds = Math.Min(roundedSeconds, 60);
        return roundedSeconds / 5;
    }

    /// <summary>
    /// Renders the shield effect on the entity sprite.
    /// </summary>
    /// <param name="uid">ID of the entity on which to show the effect</param>
    /// <param name="effect">Sprite specifier</param>
    /// <param name="loop">Should the animation loop</param>
    private void ShowShieldEffect(EntityUid uid, SpriteSpecifier effect, bool loop)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        var layer = _sprite.LayerMapReserve((uid, sprite), DamageShieldKey.Key);
        _sprite.LayerSetSprite((uid, sprite), layer, effect);
        _sprite.LayerSetVisible((uid, sprite), layer, true);
        _sprite.LayerSetAutoAnimated((uid, sprite), layer, loop);

        if (!loop)
        {
            _sprite.LayerSetAnimationTime((uid, sprite), layer, 0);
        }
    }

    private void HideShieldEffect(EntityUid uid)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        if (_sprite.LayerMapTryGet((uid, sprite), DamageShieldKey.Key, out var layer, false))
        {
            _sprite.LayerSetVisible((uid, sprite), layer, false);
        }

        _isHolding = false;
    }

    /// <summary>
    /// If the user takes damage while charging the shield, the shield will decay.
    /// </summary>
    private void CheckDamageInterruption(EntityUid user)
    {
        if (!TryComp<DamageableComponent>(user, out var damageComp))
            return;

        if (!_lastTotalDamage.TryGetValue(user, out var lastDamage))
        {
            _lastTotalDamage[user] = damageComp.TotalDamage;
            return;
        }

        var damageReceived = damageComp.TotalDamage - lastDamage;

        if (damageReceived > FixedPoint2.New(1.0))
        {
            HideShieldEffect(user);

            _isChargingEffectActive = false;

            if (TryGetSmasherInHands(user, out var _, out var smasher) &&
                smasher.EffectDecay != null)
            {
                ShowShieldEffect(user, smasher.EffectDecay, false);
                _isDecayEffectActive = true;
                _decayEndTime = _timing.CurTime + TimeSpan.FromSeconds(1.8);
            }

            RaiseNetworkEvent(new ShieldChargingEndEvent(GetNetEntity(user)));

            _isHolding = false;

            Log.Debug("!!!!!!!!!!!!!!!!!!!!!!!!!!! Зарядка прервана из-за урона");

            _lastTotalDamage.Remove(user);
            return;
        }

        _lastTotalDamage[user] = damageComp.TotalDamage;
    }
}
