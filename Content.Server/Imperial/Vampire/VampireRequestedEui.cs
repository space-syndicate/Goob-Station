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
    private readonly VampireSystem _vampireSystem;

    public VampireRequestedEui(EntityUid uid, IEntityManager entityManager, SharedActionsSystem actions, VampireSystem vampireSystem)
    {
        _uid = uid;
        _entityManager = entityManager ?? throw new ArgumentNullException(nameof(entityManager));
        _actions = actions ?? throw new ArgumentNullException(nameof(actions));
        _vampireSystem = vampireSystem ?? throw new ArgumentNullException(nameof(vampireSystem));
    }

    public override void HandleMessage(EuiMessageBase msg)
    {
        if (msg is not VampireRequestedEuiMessage request)
            return;

        GrantAbilities(_uid, request.SelectionNumber);
        Close();
    }

    public void GrantAbilities(EntityUid uid, int selection)
    {
        var vamp = _entityManager.EnsureComponent<VampireComponent>(uid);
        vamp.DirectionSelected = true;
        vamp.SelectedSubgroup = selection;
        _vampireSystem.SetBloodCounterAlert(uid);

        // выдача уникальных способностей в зависимости от группы
        var uniqueActions = selection switch
        {
            1 => VampireAbilityLists.Hemomancer,
            2 => VampireAbilityLists.Umbrae,
            3 => VampireAbilityLists.Gargantua,
            _ => new List<EntProtoId>()
        };

        for (int i = 0; i < uniqueActions.Count; i++)
        {
            if (VampireAbilityLists.AbilityThresholds.TryGetValue(i, out var threshold))
            {
                if (vamp.TotalDrunk >= threshold && !vamp.UnlockedAbilityIndices.Contains(i))
                {
                    EntityUid? actionEnt = null;
                    _actions.AddAction(uid, ref actionEnt, uniqueActions[i]);

                    if (actionEnt != null)
                    {
                        vamp.GrantedActions.Add(actionEnt.Value);
                        vamp.UnlockedAbilityIndices.Add(i);
                    }
                }
            }
        }

        _entityManager.Dirty(uid, vamp);
    }
}
