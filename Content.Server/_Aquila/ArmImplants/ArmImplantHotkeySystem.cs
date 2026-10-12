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

namespace Content.Server._Aquila.ArmImplants;

public sealed class ArmImplantHotkeySystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedBodySystem _body = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;

    public override void Initialize()
    {
        base.Initialize();

        CommandBinds.Builder
            .Bind(ContentKeyFunctions.UseItemInHand,
                InputCmdHandler.FromDelegate(OnUseItemInHand, handle: false, outsidePrediction: false))
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
            || !_hands.TryGetHand((user, hands), hands.ActiveHandId, out var hand))
            return;

        var held = _hands.GetHeldItem((user, hands), hands.ActiveHandId);
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

            if (held != null)
                continue;

            foreach (var (organId, _) in _body.GetPartOrgans(partId))
            {
                if (!HasComp<AugmentToolPanelComponent>(organId)
                    || !TryComp<AugmentActionComponent>(organId, out var augmentAction))
                    continue;

                TryPerformAction(user, augmentAction.ActionEntity);
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
