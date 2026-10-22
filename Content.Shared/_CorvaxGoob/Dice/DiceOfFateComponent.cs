// CorvaxGoob-DiceOfFate : Corvax Wega Port (original author: Zekins3366)

using Robust.Shared.GameStates;

namespace Content.Shared._CorvaxGoob.Dice;

[RegisterComponent, NetworkedComponent]
public sealed partial class DiceOfFateComponent : Component
{
    [DataField]
    public bool Used;
}
