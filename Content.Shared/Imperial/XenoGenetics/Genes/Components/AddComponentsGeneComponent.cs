using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.Imperial.XenoGenetics.Genes.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class AddComponentsGeneComponent : Component
{
    [DataField(required: true)]
    public ComponentRegistry Components = new();

    [DataField, AutoNetworkedField]
    public float Threshold = 70f;

}

[RegisterComponent, NetworkedComponent]
public sealed partial class AddComponentsNoRandomGeneComponent : Component
{
    [DataField(required: true)]
    public ComponentRegistry Components = new();

}