using Content.Client.Imperial.TargetOverlay;
using Content.Client.Imperial.XxRaay.UI;
using Content.Shared.Imperial.TargetOverlay;
using Content.Shared.Imperial.TargetOverlay.Events;
using Content.Shared.Imperial.XxRaay.Components;
using Content.Client.Weapons.Ranged.Systems;
using Content.Client.Weapons.Ranged.Components;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.GameStates;
using Robust.Shared.Input;
using Robust.Shared.Input.Binding;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.GameObjects;
using System.Linq;

namespace Content.Client.Imperial.XxRaay.Systems;

/// <summary>
/// Клиентская система для обработки плечевой ракетной установки.
/// </summary>
public sealed class ShoulderRocketLauncherClientSystem : EntitySystem
{
    [Dependency] private readonly SharedTargetOverlaySystem _targetOverlaySystem = default!;
    [Dependency] private readonly IComponentFactory _componentFactory = default!;
    [Dependency] private readonly IOverlayManager _overlayManager = default!;
    [Dependency] private readonly SharedHandsSystem _handsSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        
        SubscribeLocalEvent<ShoulderRocketLauncherComponent, GunSystem.UpdateAmmoCounterEvent>(OnAmmoCounterUpdate);
        SubscribeLocalEvent<ShoulderRocketLauncherComponent, GunSystem.AmmoCounterControlEvent>(OnAmmoCounterControl);
        SubscribeLocalEvent<ShoulderRocketLauncherComponent, AfterAutoHandleStateEvent>(OnAfterAutoHandleState);
        
        CommandBinds.Builder
            .Bind(EngineKeyFunctions.UseSecondary, new PointerInputCmdHandler(OnMouseRightPressed))
            .Register<ShoulderRocketLauncherClientSystem>();
    }

    private void OnAfterAutoHandleState(EntityUid uid, ShoulderRocketLauncherComponent component, ref AfterAutoHandleStateEvent args)
    {
        if (TryComp<AmmoCounterComponent>(uid, out var ammoCounter) && ammoCounter.Control != null)
        {
            var ev = new GunSystem.UpdateAmmoCounterEvent()
            {
                Control = ammoCounter.Control
            };
            RaiseLocalEvent(uid, ev, false);
        }
    }

    private void OnAmmoCounterControl(EntityUid uid, ShoulderRocketLauncherComponent component, GunSystem.AmmoCounterControlEvent args)
    {
        args.Control = new RocketLauncherStatusControl();
    }

    private void OnAmmoCounterUpdate(EntityUid uid, ShoulderRocketLauncherComponent component, GunSystem.UpdateAmmoCounterEvent args)
    {
        if (args.Control is RocketLauncherStatusControl control)
        {
            control.Update(component.Charges, component.MaxCharges);
        }
    }

    private bool OnMouseRightPressed(ICommonSession? playerSession, EntityCoordinates coordinates, EntityUid entity)
    {
        if (playerSession?.AttachedEntity is not { Valid: true } player || !Exists(player))
            return false;

        if (!TryComp<TargetOverlayComponent>(player, out var targetOverlayComponent))
            return false;

        if (targetOverlayComponent.Sender == null)
            return false;

        var sender = targetOverlayComponent.Sender.Value;

        if (!TryComp<HandsComponent>(player, out var hands))
            return false;

        if (!_handsSystem.TryGetActiveItem((player, hands), out var activeItem))
            return false;

        if (activeItem != sender)
            return false;
        
        if (!TryComp<ShoulderRocketLauncherComponent>(sender, out var launcherComponent))
            return false;
        
        var maxTargetCount = Math.Min(launcherComponent.Charges, launcherComponent.MaxCharges);
        var whiteList = new HashSet<string>();
        var blackList = new HashSet<string>();
        
        foreach (var compType in targetOverlayComponent.WhiteListComponents)
        {
            whiteList.Add(_componentFactory.GetComponentName(compType));
        }
        
        foreach (var compType in targetOverlayComponent.BlackListComponents)
        {
            blackList.Add(_componentFactory.GetComponentName(compType));
        }
        
        _targetOverlaySystem.StopTargeting(player);
        _targetOverlaySystem.StartTargeting(player, sender, maxTargetCount, whiteList, blackList);

        return false;
    }
}

