using Robust.Shared.GameStates;

namespace Content.Shared.Imperial.Vampire;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class VampireJerkOnContactComponent : Component
{
    [DataField, AutoNetworkedField]
    public TimeSpan Knockdown;

    [DataField, AutoNetworkedField]
    public int Damage;
}
