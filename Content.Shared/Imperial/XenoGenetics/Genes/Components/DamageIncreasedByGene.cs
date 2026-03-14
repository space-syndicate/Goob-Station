using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using Content.Shared.Damage;

namespace Content.Shared.Imperial.XenoGenetics.Genes.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class DamageIncreasedByGeneComponent : Component
{

    [DataField, AutoNetworkedField]
    public DamageSpecifier BonusDamage;

    [DataField, AutoNetworkedField]
    public float BonusAttackRate;
}