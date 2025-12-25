using Content.Client.Imperial.RadialMenu;
using Content.Shared.Doors.Components;
using Content.Shared.Imperial.XxRaay.Nda079;
using Content.Shared.Imperial.XxRaay.Nda079.Events;
using Robust.Client.Player;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.IoC;
using Robust.Shared.Timing;

namespace Content.Client.Imperial.XxRaay.Nda079;

public sealed class NDA079AirlockAbilitySystem : SharedNDA079AirlockAbilitySystem
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;

    [Dependency] private readonly IGameTiming _timing = default!;

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

        var now = _timing.CurTime;
        if (now < _lastUiOpenTime + TimeSpan.FromSeconds(0.5))
            return;

        _lastUiOpenTime = now;

        OpenRadialMenu(user, target);
    }

    private void EnsureIconsLoaded()
    {
        if (_iconsLoaded)
            return;

        var cache = IoCManager.Resolve<IResourceCache>();
        var doorsRsi = cache.GetResource<RSIResource>("/Textures/Imperial/XxRaay/079/doors.rsi").RSI;

        if (doorsRsi.TryGetState("oping", out var openState))
            _iconOpen = openState.Frame0;

        if (doorsRsi.TryGetState("closing", out var closeState))
            _iconClose = closeState.Frame0;

        if (doorsRsi.TryGetState("bolting", out var boltState))
            _iconBolt = boltState.Frame0;

        _iconsLoaded = true;
    }

    private void OpenRadialMenu(EntityUid user, EntityUid target)
    {
        _activeRadial?.Close();

        var radial = new RadialContainer();
        _activeRadial = radial;

        if (!TryComp<DoorComponent>(target, out var door))
            return;

        EnsureIconsLoaded();

        var isOpen = door.State == DoorState.Open || door.State == DoorState.Opening;

        var toggleText = isOpen ? "Закрыть" : "Открыть";
        Texture? toggleIcon = isOpen ? _iconClose : _iconOpen;

        var toggleButton = toggleIcon != null
            ? radial.AddButton(toggleText, toggleIcon)
            : radial.AddButton(toggleText);

        toggleButton.Controller.OnPressed += (_) =>
        {
            SendActionToServer(user, target, NDA079AirlockActionType.Toggle);
            radial.Close();
        };

        var boltButton = _iconBolt != null
            ? radial.AddButton("Заболтовать", _iconBolt)
            : radial.AddButton("Заболтовать");
        boltButton.Controller.OnPressed += (_) =>
        {
            SendActionToServer(user, target, NDA079AirlockActionType.Bolt);
            radial.Close();
        };

        radial.Closed += () =>
        {
            if (_activeRadial == radial)
                _activeRadial = null;

            radial.Dispose();
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
