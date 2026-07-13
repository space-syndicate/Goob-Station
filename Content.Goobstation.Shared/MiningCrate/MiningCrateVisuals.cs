// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Serialization;

namespace Content.Goobstation.Shared.MiningCrate;

[Serializable, NetSerializable]
public enum MiningCrateVisuals : byte
{
    Display,
}

[Serializable, NetSerializable]
public enum MiningCrateLayers : byte
{
    Main,
}

[Serializable, NetSerializable]
public enum MiningCrateState : byte
{
    Off = 0,
    Locked = 1,
    Unlocked = 2,
    Open = 3,
}

[Serializable, NetSerializable]
public enum MiningCrateWireStatus : byte
{
    SecurityIndicator,
    LockIndicator,
    SirenIndicator,
    BoomIndicator,
}
