// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Goobstation.Shared.MiningCrate;

/// <summary>
/// Raised on a mining crate when a player tries to unlock it (click / verb).
/// Unlock modules (points, free timer, future methods) should handle this and set <see cref="Handled"/>.
/// </summary>
[ByRefEvent]
public record struct MiningCrateTryUnlockEvent(EntityUid User, bool Handled = false);

/// <summary>
/// Raised after a crate has been successfully unlocked (loot filled, lock open, security disarmed).
/// </summary>
[ByRefEvent]
public record struct MiningCrateUnlockedEvent(EntityUid? User = null);

/// <summary>
/// Forces the crate power switch off (e.g. after shock). Handled server-side.
/// </summary>
[ByRefEvent]
public record struct MiningCrateForcePowerOffEvent(bool Handled = false);
