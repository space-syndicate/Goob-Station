using Content.Shared.Chemistry.Components;
using Robust.Shared.Serialization;

namespace Content.Shared.Imperial.ImperialBorgs;

[DataDefinition]
[Serializable, NetSerializable]
public sealed partial class BorgHypoSolution
{
    [DataField("reagents")]
    public List<ImperialBorgsReagent> Reagents = new();

    public string? GetPrimaryReagentId()
    {
        return Reagents.Count > 0 ? Reagents[0].ReagentId : null;
    }

    public Solution ToChemSolution()
    {
        var solution = new Solution();
        foreach (var reagent in Reagents)
        {
            solution.AddReagent(reagent.ReagentId, reagent.Quantity);
        }
        return solution;
    }
}
