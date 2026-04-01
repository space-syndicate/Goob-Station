using Content.Shared.Imperial.Atmospheric.RCD.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared.Imperial.Atmospheric.RCD.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(AtmosphericRCDAmmoSystem))]
public sealed partial class AtmosphericRCDAmmoComponent : Component
{
    /// <summary>
    /// How many charges are contained in this ammo cartridge.
    /// Can be partially transferred into an RCD, until it is empty then it gets deleted.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int Charges = 40;
}
