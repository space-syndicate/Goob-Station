using Content.Shared.Imperial.MiningWeapons.Smasher;
using Content.Shared.Imperial.MiningWeapons.Smasher.Events;
using Content.Shared.Imperial.MiningWeapons.Smasher.Enums;
using Robust.Shared.Input;
using Robust.Shared.Utility;
using Robust.Client.GameObjects;
using Robust.Client.Player;

namespace Content.Client.Imperial.MiningWeapons.Smasher;

public sealed class SmasherSystem : SharedSmasherSystem
{
    [Dependency] private readonly InputSystem _inputSystem = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;
    [Dependency] private readonly IPlayerManager _player = default!;

    private BoundKeyState _lastKeyState = BoundKeyState.Up; // To track changes in the state of a key

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<ShowShieldEffectEvent>(OnShowShieldEffect);
        SubscribeNetworkEvent<HideShieldEffectEvent>(OnHideShieldEffect);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var user = _player.LocalEntity;
        if (user == null)
            return;

        var useKey = EngineKeyFunctions.UseSecondary;
        var keyState = _inputSystem.CmdStates.GetState(useKey);

        if (keyState != _lastKeyState)
        {
            _lastKeyState = keyState;

            if (TryGetSmasherInHands(user.Value, out var smasherUid, out var smasherComp))
            {
                RaiseNetworkEvent(new UpdateSmasherStateEvent(
                    GetNetEntity(user.Value),
                    GetNetEntity(smasherUid.Value),
                    keyState
                ));
            }
        }
    }

    private void OnShowShieldEffect(ShowShieldEffectEvent ev)
    {
        ShowShieldEffect(GetEntity(ev.Uid), ev.EffectDecay, ev.Loop);
    }

    private void OnHideShieldEffect(HideShieldEffectEvent ev)
    {
        HideShieldEffect(GetEntity(ev.Uid));
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

        if (loop)
        {
            _sprite.LayerSetAutoAnimated((uid, sprite), layer, true);
        }
        else
        {
            _sprite.LayerSetAutoAnimated((uid, sprite), layer, true);
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
    }
}
