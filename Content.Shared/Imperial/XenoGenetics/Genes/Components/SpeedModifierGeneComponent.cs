using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Imperial.XenoGenetics.Genes.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SpeedModifierGeneComponent : Component
{
    /// <summary>
    /// То насколько увелчится скорость при вводе гена. Считается по формуле скорость_энтити * speedModifier + geneMultiplier
    /// </summary>
    [DataField, AutoNetworkedField]
    public float speedModifier = 0.30f;
    
    [DataField, AutoNetworkedField]
    public bool randomizeModifier = true;
}