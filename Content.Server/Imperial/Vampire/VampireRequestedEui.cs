using Content.Server.EUI;
using Content.Shared.Actions;
using Content.Shared.Eui;
using Content.Shared.Imperial.Vampire;
using Robust.Shared.Prototypes;

namespace Content.Server.Imperial.Vampire;

public sealed class VampireRequestedEui : BaseEui
{
    private readonly IEntityManager _entityManager = default!;
    private readonly EntityUid _uid;
    private readonly SharedActionsSystem _actions = default!;
    private readonly SharedVampireSystem _vampireSystem = default!;
    private readonly IPrototypeManager _prototypeManager = default!;

    public VampireRequestedEui(EntityUid uid, IEntityManager entityManager, SharedActionsSystem actions,
    SharedVampireSystem vampireSystem, IPrototypeManager prototypeManager)
    {
        _uid = uid;
        _entityManager = entityManager;
        _actions = actions;
        _vampireSystem = vampireSystem;
        _prototypeManager = prototypeManager;
    }

    public override void HandleMessage(EuiMessageBase msg)
    {
        if (msg is not VampireRequestedEuiMessage request)
            return;

        if (!_entityManager.TryGetComponent(_uid, out VampireComponent? vamp))
            return;

        GrantAbilities(_uid, request.Selection);
        Close();
        _actions.RemoveAction(_uid, vamp.SelectingSubgroupActionEntity);
    }

    public void GrantAbilities(EntityUid uid, VampireAbilityType selection)
    {
        if (uid != _uid)
            return;

        if (!_entityManager.TryGetComponent(_uid, out VampireComponent? vamp))
            return;

        if (!_prototypeManager.TryIndex<VampireAbilityListPrototype>(vamp.VampireAbilitiesID[VampireAbilityType.Hemomancer], out var hemomancer) ||
            !_prototypeManager.TryIndex<VampireAbilityListPrototype>(vamp.VampireAbilitiesID[VampireAbilityType.Umbrae], out var umbrae) ||
            !_prototypeManager.TryIndex<VampireAbilityListPrototype>(vamp.VampireAbilitiesID[VampireAbilityType.Gargantua], out var gargantua))
            return;

        var selected = selection switch
        {
            VampireAbilityType.Hemomancer => hemomancer,
            VampireAbilityType.Umbrae => umbrae,
            VampireAbilityType.Gargantua => gargantua,
            _ => null
        };

        if (selected == null)
            return;

        vamp.DirectionSelected = true;
        vamp.SelectedSubgroup = selection;
        _vampireSystem.SetBloodCounterAlert(uid);

        for (var i = 0; i < selected.Abilities.Count; i++)
        {
            if (!selected.Thresholds.TryGetValue(i, out var threshold) || vamp.TotalDrunk < threshold
                || vamp.UnlockedAbilityIndices.Contains(i))
                continue;

            EntityUid? actionEnt = null;
            _actions.AddAction(uid, ref actionEnt, selected.Abilities[i]);

            if (actionEnt != null)
            {
                vamp.GrantedActions.Add(actionEnt.Value);
                vamp.UnlockedAbilityIndices.Add(i);
            }
        }

        _entityManager.Dirty(uid, vamp);
    }

}
