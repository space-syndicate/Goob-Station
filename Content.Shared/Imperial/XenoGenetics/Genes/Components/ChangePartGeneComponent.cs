using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using Content.Shared.Humanoid;

namespace Content.Shared.Imperial.XenoGenetics.Genes.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class ChangePartGeneComponent : Component
{

    [DataField, AutoNetworkedField]
    public SpriteSpecifier? sprite = null;

    [DataField, AutoNetworkedField]
    public SpriteSpecifier? spriteOrig = null;

    [DataField, AutoNetworkedField]

    public HumanoidVisualLayers layer = HumanoidVisualLayers.Head;
}