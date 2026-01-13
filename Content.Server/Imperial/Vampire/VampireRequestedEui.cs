using System.Runtime.CompilerServices;
using Content.Server.EUI;
using Content.Shared.Actions;
using Content.Shared.Eui;
using Content.Shared.Imperial.Vampire;
using Robust.Shared.Prototypes;
using DependencyAttribute = Robust.Shared.IoC.DependencyAttribute;

namespace Content.Server.Imperial.Vampire;

public sealed class VampireRequestedEui : BaseEui
{
    private readonly EntityUid _uid;
    private readonly IEntityManager _entityManager;
    private readonly SharedActionsSystem _actions;
    private readonly SharedVampireSystem _vampireSystem;
    private readonly VampireComponent _vampireComponent;

    public VampireRequestedEui(EntityUid uid, IEntityManager entityManager, SharedActionsSystem actions, SharedVampireSystem vampireSystem,
    VampireComponent vampireComponent)
    {
        _uid = uid;
        _entityManager = entityManager ?? throw new ArgumentNullException(nameof(entityManager));
        _actions = actions ?? throw new ArgumentNullException(nameof(actions));
        _vampireSystem = vampireSystem ?? throw new ArgumentNullException(nameof(vampireSystem));
        _vampireComponent = vampireComponent ?? throw new ArgumentNullException(nameof(vampireComponent));
    }

    public override void HandleMessage(EuiMessageBase msg)
    {
        if (msg is not VampireRequestedEuiMessage request)
            return;

        GrantAbilities(_uid, request.SelectionNumber);
        Close();
        _actions.RemoveAction(_uid, _vampireComponent.SelectingSubgroupActionEntity);
    }

    public void GrantAbilities(EntityUid uid, int selection)
    {
        if (uid != _uid)
            return;

        // выдача уникальных способностей в зависимости от группы
        var uniqueActions = selection switch
        {
            1 => VampireAbilityLists.Hemomancer,
            2 => VampireAbilityLists.Umbrae,
            3 => VampireAbilityLists.Gargantua,
            _ => null
        };

        if (uniqueActions == null)
            return;

        _vampireComponent.DirectionSelected = true;
        _vampireComponent.SelectedSubgroup = selection;
        _vampireSystem.SetBloodCounterAlert(uid);

        for (int i = 0; i < uniqueActions.Count; i++)
        {
            if (VampireAbilityLists.AbilityThresholds.TryGetValue(i, out var threshold))
            {
                if (_vampireComponent.TotalDrunk >= threshold && !_vampireComponent.UnlockedAbilityIndices.Contains(i))
                {
                    EntityUid? actionEnt = null;
                    _actions.AddAction(uid, ref actionEnt, uniqueActions[i]);

                    if (actionEnt != null)
                    {
                        _vampireComponent.GrantedActions.Add(actionEnt.Value);
                        _vampireComponent.UnlockedAbilityIndices.Add(i);
                    }
                }
            }
        }

        _entityManager.Dirty(uid, _vampireComponent);
    }
}
