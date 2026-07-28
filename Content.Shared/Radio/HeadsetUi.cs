// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Radio;

[Serializable, NetSerializable]
public enum HeadsetUiKey : byte
{
    Key,
}

[Serializable, NetSerializable]
public sealed class HeadsetBoundUserInterfaceState(List<HeadsetChannelState> channels) : BoundUserInterfaceState
{
    public readonly List<HeadsetChannelState> Channels = channels;
}

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

[Serializable, NetSerializable]
public sealed class ToggleHeadsetChannelMessage(ProtoId<RadioChannelPrototype> channel, bool enabled)
    : BoundUserInterfaceMessage
{
    public readonly ProtoId<RadioChannelPrototype> Channel = channel;
    public readonly bool Enabled = enabled;
}

[Serializable, NetSerializable]
public sealed class ToggleHeadsetChannelSoundMessage(ProtoId<RadioChannelPrototype> channel, bool enabled)
    : BoundUserInterfaceMessage
{
    public readonly ProtoId<RadioChannelPrototype> Channel = channel;
    public readonly bool Enabled = enabled;
}
