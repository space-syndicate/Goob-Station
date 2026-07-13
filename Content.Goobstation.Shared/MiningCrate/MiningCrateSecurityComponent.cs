// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Goobstation.Shared.MiningCrate;

/// <summary>
/// Wire panel security for mining crates. While armed, damage and wrong wires can self-destruct.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class MiningCrateSecurityComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool Armed = true;

    [DataField, AutoNetworkedField]
    public bool LockWireCut;

    [DataField, AutoNetworkedField]
    public bool SirenWireIntact = true;

    [DataField, AutoNetworkedField]
    public bool Detonating;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan DetonateAt = TimeSpan.Zero;

    [DataField, AutoNetworkedField]
    public TimeSpan DetonateDelay = TimeSpan.FromSeconds(4);

    [DataField]
    public int DetonateDamage = 500;

    [DataField]
    public float StoredTotalIntensity = 50f;

    [DataField]
    public bool IntensityCaptured;
}
