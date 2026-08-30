// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Audio;

namespace Content.Shared.Radio;

public sealed partial class RadioChannelPrototype
{
    /// <summary>
    /// Sound played locally for a player when they receive a message on this radio channel.
    /// </summary>
    [DataField]
    public SoundSpecifier? ReceiveSound = null;
}
