// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._Lavaland.MiningCrate;

/// <summary>
/// Timed unlock progress for a mining crate.
/// Used by free unlock and after a successful payment module (e.g. mining points).
/// Without this component the crate cannot start a unlock countdown (only startUnlocked / emag / etc.).
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
[Access(typeof(LavalandMiningCrateSystem), typeof(MiningCrateUnlockTimerSystem), typeof(MiningCratePointsUnlockSystem))]
public sealed partial class MiningCrateUnlockTimerComponent : Component
{
    /// <summary>
    /// Payment or free-unlock already accepted; waiting for <see cref="UnlockAt"/>.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Started;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan UnlockAt = TimeSpan.Zero;

    [DataField, AutoNetworkedField]
    public TimeSpan UnlockDelay = TimeSpan.FromSeconds(5);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextUnlockBlink = TimeSpan.Zero;

    [DataField, AutoNetworkedField]
    public bool UnlockBlinkShowUnlocked;

    [DataField]
    public TimeSpan UnlockBlinkInterval = TimeSpan.FromSeconds(1);
}
