using Content.Shared.Imperial.Lavaland.MiningWeapons.Systems;
using Content.Shared.Imperial.Lavaland.MiningWeapons.Events;
using Content.Shared.Imperial.Lavaland.MiningWeapons.Components;
using Content.Shared.Imperial.Lavaland.MiningWeapons.Enums;
using Robust.Client.Player;
using Robust.Shared.Timing;
using Robust.Shared.Input;
using Robust.Client.GameObjects;
using Content.Shared.CombatMode;

namespace Content.Client.Imperial.Lavaland.MiningWeapons;

public sealed class SmasherSystem : SharedSmasherSystem
{
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly InputSystem _inputSystem = default!;
    [Dependency] private readonly SharedCombatModeSystem _combatMode = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;
    private TimeSpan _holdStartTime;
    private bool _isHolding;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ShieldActiveComponent, ComponentStartup>(OnShieldStartup);
        SubscribeLocalEvent<ShieldActiveComponent, ComponentShutdown>(OnShieldShutdown);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var user = _player.LocalEntity;
        if (user == null) return;

        if (!TryGetSmasherInHands(user.Value, out var smasherUid, out var smasherComp))
        {
            if (_isHolding)
            {
                Log.Debug("Предмет убран из рук, сбрасываем таймер");
                _isHolding = false;
            }
            return;
        }

        var useKey = EngineKeyFunctions.UseSecondary;

        if (_inputSystem.CmdStates.GetState(useKey) == BoundKeyState.Down)
        {
            if (!_isHolding)
            {
                if (!_combatMode.IsInCombatMode(user))
                {
                    Log.Debug("Игрок не в боевом режиме!");
                    return;
                }
                _holdStartTime = _timing.CurTime;
                _isHolding = true;
                Log.Debug($"Начато удержание ПКМ. Время: {_timing.CurTime}");
            }

            var holdTime = (_timing.CurTime - _holdStartTime).TotalSeconds;
            Log.Debug($"Удерживаем ПКМ: {holdTime:F1} сек");

            if (holdTime >= 2.5f)
            {
                Log.Debug("Удержание достигло 2.5 сек! Отправляем событие на сервер.");
                RaisePredictiveEvent(new ShieldActivatedEvent(GetNetEntity(smasherUid.Value)));
                _isHolding = false;
            }
        }
        else if (_isHolding)
        {
            _isHolding = false;
            Log.Debug("ПКМ отпущена, сбрасываем таймер");
        }
    }

    private void OnShieldStartup(EntityUid uid, ShieldActiveComponent component, ComponentStartup args)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        var layer = _sprite.LayerMapReserve((uid, sprite), DamageShieldKey.Key);

        if (component.Effect != null)
        {
            _sprite.LayerSetSprite((uid, sprite), layer, component.Effect);
            _sprite.LayerSetVisible((uid, sprite), layer, true);
        }
    }

    private void OnShieldShutdown(EntityUid uid, ShieldActiveComponent component, ComponentShutdown args)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        if (_sprite.LayerMapTryGet((uid, sprite), DamageShieldKey.Key, out var layer, false))
        {
            _sprite.LayerSetVisible((uid, sprite), layer, false);
        }
    }
}
