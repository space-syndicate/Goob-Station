using Content.Client.Imperial.RadialMenu;
using Content.Shared.Doors.Components;
using Content.Shared.Imperial.XxRaay.Nda079;
using Content.Shared.Imperial.XxRaay.Nda079.Events;
using Robust.Client.Player;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Client.Imperial.XxRaay.Nda079;

public sealed class NDA079AirlockAbilitySystem : SharedNDA079AirlockAbilitySystem
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IResourceCache _resourceCache = default!;

    private RadialContainer? _activeRadial;
    private TimeSpan _lastUiOpenTime;

    private Texture? _iconOpen;
    private Texture? _iconClose;
    private Texture? _iconBolt;
    private bool _iconsLoaded;

    protected override void OnAirlockVerbAct(EntityUid user, EntityUid target)
    {
        var localPlayer = _playerManager.LocalEntity;
        if (localPlayer != user)
            return;

        if (!TryComp<NDA079AirlockAbilityComponent>(user, out var abilityComp))
            return;

        var now = _timing.CurTime;
        if (now < _lastUiOpenTime + abilityComp.UiCooldown)
            return;

        _lastUiOpenTime = now + abilityComp.UiCooldown;

        OpenRadialMenu(user, target, abilityComp);
    }

    private void EnsureIconsLoaded(ResPath doorsRsiPath)
    {
        if (_iconsLoaded)
            return;

        var doorsRsi = _resourceCache.GetResource<RSIResource>(doorsRsiPath).RSI;

        if (doorsRsi.TryGetState("oping", out var openState))
            _iconOpen = openState.Frame0;

        if (doorsRsi.TryGetState("closing", out var closeState))
            _iconClose = closeState.Frame0;

        if (doorsRsi.TryGetState("bolting", out var boltState))
            _iconBolt = boltState.Frame0;

        _iconsLoaded = true;
    }

    private void OpenRadialMenu(EntityUid user, EntityUid target, NDA079AirlockAbilityComponent abilityComp)
    {
        _activeRadial?.Close();

        var radial = new RadialContainer();
        _activeRadial = radial;

        if (!TryComp<DoorComponent>(target, out var door))
            return;

        EnsureIconsLoaded(abilityComp.DoorsRsiPath);

        var isOpen = door.State == DoorState.Open || door.State == DoorState.Opening;

        var toggleText = isOpen 
            ? Loc.GetString("nda079-ability-airlock-close")
            : Loc.GetString("nda079-ability-airlock-open");
        Texture? toggleIcon = isOpen ? _iconClose : _iconOpen;

        var toggleButton = toggleIcon != null
            ? radial.AddButton(toggleText, toggleIcon)
            : radial.AddButton(toggleText);

        toggleButton.Controller.OnPressed += (_) =>
        {
            SendActionToServer(user, target, NDA079AirlockActionType.Toggle);
            radial.Close();
        };

        var boltText = Loc.GetString("nda079-ability-airlock-bolt");
        var boltButton = _iconBolt != null
            ? radial.AddButton(boltText, _iconBolt)
            : radial.AddButton(boltText);
        boltButton.Controller.OnPressed += (_) =>
        {
            SendActionToServer(user, target, NDA079AirlockActionType.Bolt);
            radial.Close();
        };

        radial.Closed += () =>
        {
            if (_activeRadial == radial)
                _activeRadial = null;
        };

        radial.OpenAttached(target);
    }

    private void SendActionToServer(EntityUid user, EntityUid target, NDA079AirlockActionType actionType)
    {
        var ev = new NDA079AirlockActionEvent
        {
            Target = GetNetEntity(target),
            ActionType = actionType
        };

        RaiseNetworkEvent(ev);
    }
}
