using Content.Goobstation.Server.Augments;
using Content.Goobstation.Shared.Augments;
using Content.Goobstation.Shared.MantisBlades;
using Content.Shared.Actions;
using Content.Shared.Body.Part;
using Content.Shared.Body.Systems;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Input;
using Robust.Shared.Input.Binding;
using Robust.Shared.Player;

namespace Content.Goobstation.Server._Aquila.ArmImplants;

public sealed class ArmImplantHotkeySystem : EntitySystem
{
    [Dependency] private readonly AugmentPowerCellSystem _augmentPowerCell = default!;
    [Dependency] private readonly AugmentToolPanelSystem _toolPanel = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedBodySystem _body = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;

    public override void Initialize()
    {
        base.Initialize();

        CommandBinds.Builder
            .BindBefore(ContentKeyFunctions.UseItemInHand,
                InputCmdHandler.FromDelegate(OnUseItemInHand, handle: false, outsidePrediction: false),
                typeof(SharedHandsSystem))
            .Register<ArmImplantHotkeySystem>();
    }

    public override void Shutdown()
    {
        base.Shutdown();

        CommandBinds.Unregister<ArmImplantHotkeySystem>();
    }

    private void OnUseItemInHand(ICommonSession? session)
    {
        if (session?.AttachedEntity is not { } user
            || !TryComp<HandsComponent>(user, out var hands)
            || hands.ActiveHandId is not { } handId
            || !_hands.TryGetHand((user, hands), handId, out var hand))
            return;

        var held = _hands.GetHeldItem((user, hands), handId);
        var symmetry = hand.Value.Location switch
        {
            HandLocation.Left => BodyPartSymmetry.Left,
            HandLocation.Right => BodyPartSymmetry.Right,
            _ => BodyPartSymmetry.None,
        };

        foreach (var (partId, _) in _body.GetBodyChildrenOfType(user, BodyPartType.Arm, symmetry: symmetry))
        {
            if (TryComp<MantisBladeArmComponent>(partId, out var blade)
                && (held == null || held == blade.BladeUid))
            {
                TryPerformAction(user, blade.ActionUid);
                return;
            }

            if (held != null && !HasComp<AugmentToolPanelActiveItemComponent>(held))
                continue;

            foreach (var (organId, _) in _body.GetPartOrgans(partId))
            {
                if (!TryComp<AugmentToolPanelComponent>(organId, out var panel)
                    || !TryComp<AugmentActionComponent>(organId, out var augmentAction))
                    continue;

                if (held == null)
                {
                    TryPerformAction(user, augmentAction.ActionEntity);
                    return;
                }

                if (_augmentPowerCell.TryUseChargeBody(user, panel.SwitchCharge))
                    _toolPanel.SwitchTool((organId, panel), null, user, handId);

                return;
            }
        }
    }

    private void TryPerformAction(EntityUid user, EntityUid? action)
    {
        if (action is not { } actionUid)
            return;

        _actions.TryPerformAction(user, new RequestPerformActionEvent(GetNetEntity(actionUid)));
    }
}
