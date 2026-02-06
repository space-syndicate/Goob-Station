using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.Imperial.XenoGenetics.Genes.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class AddSpriteGeneComponent : Component
{

    [DataField, AutoNetworkedField]
    public SpriteSpecifier? sprite = null;
}