// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Serialization;

namespace Content.Shared._Lavaland.MiningCrate;

[Serializable, NetSerializable]
public enum LavalandMiningCrateVisuals : byte
{
    Display,
}

[Serializable, NetSerializable]
public enum LavalandMiningCrateVisualLayers : byte
{
    Main,
}

[Serializable, NetSerializable]
public enum LavalandMiningCrateDisplayState : byte
{
    Off = 0,
    Locked = 1,
    Unlocked = 2,
    Open = 3,
}
