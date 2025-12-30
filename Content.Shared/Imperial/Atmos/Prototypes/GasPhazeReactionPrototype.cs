using Content.Shared.Atmos;
using Robust.Shared.Prototypes;

namespace Content.Server.Imperial.Atmos.Reactions.Prototypes;

[Prototype("gasPhazeReaction"), DataDefinition]
public sealed partial class GasPhazeReactionPrototype : IPrototype
{
    [ViewVariables]
    [IdDataField]
    public string ID { get; private set; } = default!;
    [DataField]
    public float EnergyScale { get; private set; } = 1f;
    [DataField]
    public int AddedGasID { get; private set; }
    [DataField]
    public bool UseGasTwo { get; private set; } = false;
    [DataField]
    public int MissingGasID { get; private set; }
    [DataField]
    public int MissingGasIDTwo { get; private set; }
    [DataField]
    public float OutputFactor { get; private set; } = 1f;
}
