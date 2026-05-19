using Content.Shared.Damage.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Imperial.Vampire;

[RegisterComponent, AutoGenerateComponentState, NetworkedComponent]
public sealed partial class VampireBuffComponent : Component
{
    [AutoNetworkedField]
    public float BoostedDamage;

    [AutoNetworkedField]
    public float BoostSpeed;

    [AutoNetworkedField]
    public float BoostAttackRate;

    [DataField]
    public string[] BuffDamageID = new[]
    {
        "Blunt",
        "Slash"
    };
}
