using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Imperial.XenoGenetics.Genes.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RespiratorGeneComponent : Component
{
    /// <summary>
    /// Трешхолд качества генов после которого у энтити пропадает потребность в воздухе
    /// </summary>
    [DataField, AutoNetworkedField]
    public float GeneNeeded = 75f;


    [DataField, AutoNetworkedField]
    public bool Active = false;
}