using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.Imperial.XenoGenetics.Genes.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class DamageIncreaserGeneComponent : Component
{
    [DataField(required: true)]
    public ComponentRegistry DamageGene = new();

    [DataField, AutoNetworkedField]
    public bool RandomStats = true;
}