using Robust.Shared.GameStates;

namespace Content.Shared.Imperial.DecalGun;

/// <summary>
/// Handles tracking of maximum and current charge count, and provides a method to consume charges.
/// Intended to be inserted into a Decal Gun entity, enabling limited-use decal placement functionality.
/// </summary>
[RegisterComponent]
[NetworkedComponent]
public sealed partial class DecalGunMagComponent : Component
{
    /// <summary>
    /// Maximum number of charges this magazine can hold.
    /// </summary>
    [DataField]
    public int MaxCharges = 45;

    /// <summary>
    /// Current number of available charges.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public int CurrentCharges;

    /// <summary>
    /// Attempts to consume one charge from the magazine.
    /// Returns true if successful, or false if no charges remain.
    /// </summary>
    public bool TryUseCharge()
    {
        if (CurrentCharges <= 0)
            return false;

        CurrentCharges--;
        return true;
    }
}
