// SPDX-License-Identifier: MIT

using Content.Shared.Radio.Components;
using Robust.Shared.Prototypes;

namespace Content.Shared.Radio.EntitySystems;

public abstract partial class SharedHeadsetSystem
{
    // CorvaxGoob Start
    /// <summary>
    /// Prevents disabled headset channels from being selected as the default speak channel.
    /// </summary>
    private static bool IsDefaultChannelEnabled(
        HeadsetComponent component,
        ProtoId<RadioChannelPrototype> channel)
    {
        return !component.DisabledChannels.Contains(channel);
    }
    // CorvaxGoob End
}
