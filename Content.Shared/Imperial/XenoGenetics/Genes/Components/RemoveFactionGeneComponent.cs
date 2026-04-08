using Robust.Shared.Serialization;
using Robust.Shared.GameStates;

namespace Content.Shared.Imperial.XenoGenetics.Genes.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RemoveFactionGeneComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool Active = false;
}