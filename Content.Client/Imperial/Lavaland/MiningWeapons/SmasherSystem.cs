using Content.Shared.Imperial.Lavaland.MiningWeapons.Systems;
using Content.Shared.Imperial.Lavaland.MiningWeapons.Events;
using Content.Shared.Imperial.Lavaland.MiningWeapons.Components;
using Content.Shared.Imperial.Lavaland.MiningWeapons.Enums;
using Content.Shared.CombatMode;
using Robust.Shared.Timing;
using Robust.Shared.Input;
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
    private TimeSpan _holdStartTime;
    private TimeSpan _cooldownEnd;
    private bool _isHolding;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ShieldActiveComponent, ComponentShutdown>(OnShieldShutdown);
        SubscribeNetworkEvent<ShieldActivatedEvent>(OnShieldActivated);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var user = _player.LocalEntity;
        if (user == null) return;

        if (_timing.CurTime >= _cooldownEnd)
        {
            _cooldownEnd = TimeSpan.Zero;
        }

        if (!TryGetSmasherInHands(user.Value, out var smasherUid, out var _))
        {
            if (_isHolding)
                _isHolding = false;

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
                Log.Info("Начата зарядка щита");
            }

            var holdTime = (_timing.CurTime - _holdStartTime).TotalSeconds;
            Log.Debug($"Зарядка: {holdTime:F1} сек");

            if (holdTime >= 2.5f)
            {
                Log.Debug("Зарядка завершена - активируем щит");
                if (smasherUid != null)
                    RaisePredictiveEvent(new ShieldActivatedEvent(GetNetEntity(smasherUid.Value), NetEntity.Invalid, null));

                _isHolding = false;
                _cooldownEnd = _timing.CurTime + TimeSpan.FromSeconds(10);
            }
        }
        else if (_isHolding)
        {
            _isHolding = false;
            Log.Debug("Зарядка отменена");
        }
    }

    private void OnShieldActivated(ShieldActivatedEvent ev)
    {
        _isHolding = false;

        if (!TryGetEntity(ev.User, out var userUid))
            return;

        if (!TryComp<SpriteComponent>(userUid, out var sprite))
            return;

        var layer = _sprite.LayerMapReserve((userUid.Value, sprite), DamageShieldKey.Key);

        if (ev.Effect != null)
        {
            _sprite.LayerSetSprite((userUid.Value, sprite), layer, ev.Effect);
            _sprite.LayerSetVisible((userUid.Value, sprite), layer, true);
            Log.Info($"Визуал щита добавлен для {ToPrettyString(userUid.Value)}");
        }
    }

    private void OnShieldShutdown(EntityUid uid, ShieldActiveComponent component, ComponentShutdown args)
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
}
