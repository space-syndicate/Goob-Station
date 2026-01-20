using Robust.Shared.GameStates;

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
    public string BuffDamageBluntID = "Blunt";

    [DataField]
    public string BuffDamageSlashID = "Slash";
}
