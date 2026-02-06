using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Imperial.XenoGenetics.Genes.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RespiratorGeneComponent : Component
{
    /// <summary>
    /// То насколько увелчится скорость при вводе гена. Считается по формуле скорость_энтити + speedModifier * geneMultiplier
    /// </summary>
    [DataField, AutoNetworkedField]
    public float geneNeeded = 75f;
    
    [DataField(required: true)]
    public ComponentRegistry Components = new();

    [DataField, AutoNetworkedField]
    public bool active = false;
}