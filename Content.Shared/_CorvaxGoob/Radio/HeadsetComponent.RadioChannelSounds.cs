// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Radio;
using Robust.Shared.Prototypes;

namespace Content.Shared.Radio.Components;

public sealed partial class HeadsetComponent
{
    /// <summary>
    /// Radio channels disabled on this headset without removing encryption keys.
    /// </summary>
    [DataField]
    public HashSet<ProtoId<RadioChannelPrototype>> DisabledChannels = new();

    /// <summary>
    /// Radio channels that should not play receive sounds on this headset.
    /// </summary>
    [DataField]
    public HashSet<ProtoId<RadioChannelPrototype>> MutedReceiveSoundChannels = new();
}
