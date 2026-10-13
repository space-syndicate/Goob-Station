using System.Numerics;
using Robust.Shared.GameStates;

namespace Content.Shared._Aquila.Heretic;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class MansusHandComponent : Component
{
    [DataField, AutoNetworkedField]
    public Vector2 Origin;

    [DataField]
    public string ArmState = "arm_left";
}
