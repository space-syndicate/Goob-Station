using Content.Shared.Atmos;
using Robust.Shared.Prototypes;

namespace Content.Server.Imperial.Atmos.Reactions.Prototypes;

[Prototype("gasPhazeReaction"), DataDefinition]
public sealed partial class GasPhazeReactionPrototype : IPrototype
{
    [ViewVariables]
    [IdDataField]
    public string ID { get; private set; } = default!;
    [DataField("energyScale")]
    public float EnergyScale { get; private set; } = 1f;
    [DataField("addedGasID")]
    public int AddedGasID { get; private set; }
    [DataField("useGasTwo")]
    public bool UseGasTwo { get; private set; } = false;
    [DataField("missingGasID")]
    public int MissingGasID { get; private set; }
    [DataField("missingGasIDTwo")]
    public int MissingGasIDTwo { get; private set; }
    [DataField("outputFactor")]
    public float OutputFactor { get; private set; } = 1f;
}
