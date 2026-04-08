using Robust.Shared.Serialization;
using Robust.Shared.GameStates;
using System.ComponentModel.DataAnnotations;

namespace Content.Shared.Imperial.XenoGenetics.Genes.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class XenoGeneAddActionComponent : Component
{
    [DataField(required: true), AutoNetworkedField]
    public string Action = String.Empty;

    
}