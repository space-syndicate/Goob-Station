// this content is under ICLA licence, read more on https://wiki.imperialspace.net/icla
// Copyright: @crookielv

using Robust.Shared.Serialization;

namespace Content.Shared.Imperial.Crook.Visuals
{
    [Serializable, NetSerializable]
    public enum MetalDetectorVisualState
    {
        Off,
        Powered,
        Scanning,
        Alert
    }

    [Serializable, NetSerializable]
    public enum MetalDetectorVisuals
    {
        State
    }
}
