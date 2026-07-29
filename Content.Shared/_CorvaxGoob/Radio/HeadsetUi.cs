// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Radio;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._CorvaxGoob.Radio;

/// <summary>
/// UI key for per-headset radio channel controls.
/// </summary>
[Serializable, NetSerializable]
public enum HeadsetUiKey : byte
{
    Key,
}

/// <summary>
/// Current channel and receive-sound settings stored on a single headset.
/// </summary>
[Serializable, NetSerializable]
public sealed class HeadsetBoundUserInterfaceState(List<HeadsetChannelState> channels) : BoundUserInterfaceState
{
    public readonly List<HeadsetChannelState> Channels = channels;
}

/// <summary>
/// Client-facing state for one installed headset radio channel.
/// </summary>
[Serializable, NetSerializable, DataRecord]
public sealed partial class HeadsetChannelState
{
    public readonly ProtoId<RadioChannelPrototype> Channel;
    public readonly bool ChannelEnabled;
    public readonly bool SoundEnabled;

    public HeadsetChannelState(
        ProtoId<RadioChannelPrototype> channel,
        bool channelEnabled,
        bool soundEnabled)
    {
        Channel = channel;
        ChannelEnabled = channelEnabled;
        SoundEnabled = soundEnabled;
    }
}

/// <summary>
/// Toggles whether this headset can send and receive one channel.
/// </summary>
[Serializable, NetSerializable]
public sealed class ToggleHeadsetChannelMessage(ProtoId<RadioChannelPrototype> channel, bool enabled)
    : BoundUserInterfaceMessage
{
    public readonly ProtoId<RadioChannelPrototype> Channel = channel;
    public readonly bool Enabled = enabled;
}

/// <summary>
/// Toggles whether this headset plays the receive sound for one channel.
/// </summary>
[Serializable, NetSerializable]
public sealed class ToggleHeadsetChannelSoundMessage(ProtoId<RadioChannelPrototype> channel, bool enabled)
    : BoundUserInterfaceMessage
{
    public readonly ProtoId<RadioChannelPrototype> Channel = channel;
    public readonly bool Enabled = enabled;
}

/// <summary>
/// Applies one receive-sound enabled value to all currently installed channels.
/// </summary>
[Serializable, NetSerializable]
public sealed class SetAllHeadsetChannelSoundsMessage(bool enabled) : BoundUserInterfaceMessage
{
    public readonly bool Enabled = enabled;
}
