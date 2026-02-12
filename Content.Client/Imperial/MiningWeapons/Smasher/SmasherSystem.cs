using Content.Shared.Imperial.MiningWeapons.Smasher;
using Content.Shared.Imperial.MiningWeapons.Smasher.Events;
using Content.Shared.Imperial.MiningWeapons.Smasher.Enums;
using Robust.Shared.Input;
using Robust.Shared.Utility;
using Robust.Client.GameObjects;
using Robust.Shared.Input.Binding;
using static Robust.Shared.Input.Binding.PointerInputCmdHandler;
using Robust.Client.Player;

namespace Content.Client.Imperial.MiningWeapons.Smasher;

public sealed class SmasherSystem : SharedSmasherSystem
{
    [Dependency] private readonly SpriteSystem _sprite = default!;
    [Dependency] private readonly IPlayerManager _player = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeNetworkEvent<ShowShieldEffectEvent>(OnShowShieldEffect);
        SubscribeNetworkEvent<HideShieldEffectEvent>(OnHideShieldEffect);

        CommandBinds.Builder
            .Bind(EngineKeyFunctions.UseSecondary,
                new PointerInputCmdHandler(OnUseSecondary, ignoreUp: false))
            .Register<SmasherSystem>();
    }

    private bool OnUseSecondary(in PointerInputCmdArgs args)
    {
        var user = _player.LocalEntity;
        if (user == null)
            return false;

        if (!TryGetSmasherInHands(user.Value, out var _, out var smasher))
            return false;

        if (args.State != smasher.LastProcessedState)
        {
            smasher.LastProcessedState = args.State;

            RaiseNetworkEvent(new UpdateSmasherStateEvent(
                GetNetEntity(user.Value),
                args.State
            ));

            return true;
        }

        return false;
    }

    private void OnShowShieldEffect(ShowShieldEffectEvent ev)
    {
        ShowShieldEffectClient(GetEntity(ev.Uid), ev.EffectDecay, ev.Loop);
    }

    private void OnHideShieldEffect(HideShieldEffectEvent ev)
    {
        HideShieldEffectClient(GetEntity(ev.Uid));
    }

    /// <summary>
    /// Renders the shield effect on the entity sprite.
    /// </summary>
    /// <param name="uid">ID of the entity on which to show the effect</param>
    /// <param name="effect">Sprite specifier</param>
    /// <param name="loop">Should the animation loop</param>
    private void ShowShieldEffectClient(EntityUid uid, SpriteSpecifier? effect, bool loop)
    {
        HideShieldEffectClient(uid);

        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        if (effect == null)
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

    private void HideShieldEffectClient(EntityUid uid)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        if (_sprite.LayerMapTryGet((uid, sprite), DamageShieldKey.Key, out var layer, false))
        {
            _sprite.LayerSetVisible((uid, sprite), layer, false);
        }
    }
}
