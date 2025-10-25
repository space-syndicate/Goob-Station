// TODO: compatible with ShieldActiveComponent
using Robust.Shared.GameStates;

namespace Content.Shared.Imperial.Lavaland.MiningWeapons.Components;

/// <summary>
/// Added to the user when they pick up Smasher
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class SmasherUserComponent : Component
{
    /// <summary>
    /// True, when the shield is charged
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public bool CanCharging = false;

    /// <summary>
    /// True, when the shield is active
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public bool CanActived = false;
}
