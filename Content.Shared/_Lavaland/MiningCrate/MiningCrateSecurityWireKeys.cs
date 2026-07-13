// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Serialization;

namespace Content.Shared._Lavaland.MiningCrate;

[Serializable, NetSerializable]
public enum MiningCrateSecurityWireStatus : byte
{
    SecurityIndicator,
    LockIndicator,
    SirenIndicator,
    BoomIndicator,
}
