using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using Content.Shared.Damage;

namespace Content.Shared.Imperial.XenoGenetics.Genes.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class AddDamageResistanceGeneComponent : Component
{
    [DataField(required: true), AutoNetworkedField]
    public DamageModifierSet Modifiers = default!;
}