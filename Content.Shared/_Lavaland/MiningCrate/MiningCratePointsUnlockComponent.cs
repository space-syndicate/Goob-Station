// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._Lavaland.MiningCrate;

/// <summary>
/// Unlock by spending mining points. Requires <see cref="MiningCrateUnlockTimerComponent"/> for the countdown.
/// Add other payment modules later the same way (currency, access card, …).
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
[Access(typeof(MiningCratePointsUnlockSystem))]
public sealed partial class MiningCratePointsUnlockComponent : Component
{
    [DataField, AutoNetworkedField]
    public uint Cost = 500;

    [DataField, AutoNetworkedField]
    public bool EnableDenyLockout = true;

    [DataField, AutoNetworkedField]
    public TimeSpan DenyLockoutDuration = TimeSpan.FromSeconds(20);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan DeniedUntil = TimeSpan.Zero;

    [DataField, AutoNetworkedField]
    public float ShockOnFailChance = 0.12f;

    [DataField]
    public int ShockDamage = 18;

    [DataField]
    public TimeSpan ShockDuration = TimeSpan.FromSeconds(2.5);
}
