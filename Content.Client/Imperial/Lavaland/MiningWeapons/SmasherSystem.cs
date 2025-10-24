using Content.Shared.Imperial.Lavaland.MiningWeapons.Systems;
using Content.Shared.Imperial.Lavaland.MiningWeapons.Events;
using Content.Shared.Imperial.Lavaland.MiningWeapons.Components;
using Content.Shared.Imperial.Lavaland.MiningWeapons.Enums;
using Content.Shared.CombatMode;
using Content.Shared.FixedPoint;
using Content.Shared.Damage;
using Robust.Shared.Timing;
using Robust.Shared.Input;
using Robust.Shared.Utility;
using Robust.Client.GameObjects;
using Robust.Client.Player;

namespace Content.Client.Imperial.Lavaland.MiningWeapons;

public sealed class SmasherSystem : SharedSmasherSystem
{
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly InputSystem _inputSystem = default!;
    [Dependency] private readonly SharedCombatModeSystem _combatMode = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;
    private Dictionary<EntityUid, FixedPoint2> _lastTotalDamage = new();
    private TimeSpan _holdStartTime;
    private TimeSpan _cooldownEnd;
    private TimeSpan _decayEndTime;
    private bool _isHolding;
    private bool _isChargingEffectActive;
    private bool _isDecayEffectActive;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ShieldActiveComponent, ComponentShutdown>(OnShieldShutdown);
        SubscribeNetworkEvent<ShieldActivatedEvent>(OnShieldActivated);
        SubscribeNetworkEvent<ShieldChargingEvent>(OnShieldCharging);
        SubscribeNetworkEvent<ShieldDecayEvent>(OnShieldDecay);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var user = _player.LocalEntity;
        if (user == null) return;

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

        if (!TryGetSmasherInHands(user.Value, out var smasherUid, out var smasher))
        {
            if (_isHolding)
            {
                if (_isChargingEffectActive)
                {
                    HideShieldEffect(user.Value);
                    _isChargingEffectActive = false;
                }
                _isHolding = false;
            }
            return;
        }

        var useKey = EngineKeyFunctions.UseSecondary;

        if (_inputSystem.CmdStates.GetState(useKey) == BoundKeyState.Down && !HasComp<ShieldActiveComponent>(user))
        {
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
                Log.Info("Начата зарядка щита");
            }

            var holdTime = (_timing.CurTime - _holdStartTime).TotalSeconds;
            Log.Debug($"Зарядка: {holdTime:F1} сек");

            // To prevent the destruction of shield of smasher with minimal exposure to external factors (e.g. atmosphere)
            if (_isHolding && _isChargingEffectActive)
                CheckDamageInterruption(user.Value);

            if (holdTime >= 4.0f)
            {
                Log.Debug("Зарядка завершена - активируем щит");
                RaisePredictiveEvent(new ShieldActivatedEvent(GetNetEntity(smasherUid.Value), NetEntity.Invalid,
                    smasher.EffectActived, smasher.EffectCharging, smasher.EffectDecay));

                _isHolding = false;
                _cooldownEnd = _timing.CurTime + TimeSpan.FromSeconds(10);
            }
        }
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
                    _decayEndTime = _timing.CurTime + TimeSpan.FromSeconds(1.8);
                    Log.Debug("Показан эффект распада");
                }
            }

            _isHolding = false;
            Log.Debug("Зарядка отменена");
        }
    }

    private void OnShieldShutdown(EntityUid uid, ShieldActiveComponent component, ComponentShutdown args)
    {
        if (component.EffectDecay != null)
        {
            ShowShieldEffect(uid, component.EffectDecay, true);
            _isDecayEffectActive = true;
            _decayEndTime = _timing.CurTime + TimeSpan.FromSeconds(1.8);
            Log.Info($"Щит деактивирован с распадом для {ToPrettyString(uid)}");
        }
        else
        {
            HideShieldEffect(uid);
        }

        _isHolding = false;
        _isChargingEffectActive = false;
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
            _decayEndTime = _timing.CurTime + TimeSpan.FromSeconds(1.8);
            Log.Info($"Запущен распад щита для {ToPrettyString(userUid.Value)}");
        }
    }

    private void OnShieldActivated(ShieldActivatedEvent ev)
    {
        _isHolding = false;
        _isChargingEffectActive = false;

        if (!TryGetEntity(ev.User, out var userUid))
            return;

        HideShieldEffect(userUid.Value);

        if (ev.EffectActived != null)
        {
            ShowShieldEffect(userUid.Value, ev.EffectActived, true);
            Log.Info($"Активирован щит для {ToPrettyString(userUid.Value)}");
        }
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
            Log.Info($"Убран слой щита для {ToPrettyString(uid)}");
        }

        _isHolding = false;
        Log.Info($"щит убран OnShieldShutdown {ToPrettyString(uid)}");
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
            Log.Info($"Обнаружен урон: {damageReceived}. Прерываем зарядку.");
            HandleDamageInterruption(user);
            _lastTotalDamage.Remove(user);
            return;
        }

        _lastTotalDamage[user] = damageComp.TotalDamage;
    }

    private void HandleDamageInterruption(EntityUid user)
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

        _isHolding = false;

        Log.Debug("Зарядка прервана из-за урона");
    }
}
