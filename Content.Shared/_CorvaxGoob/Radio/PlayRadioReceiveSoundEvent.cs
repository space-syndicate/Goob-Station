// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Audio;
using Robust.Shared.Serialization;

namespace Content.Shared._CorvaxGoob.Radio;

/// <summary>
/// Sent to a client when a headset should play a local radio receive sound.
/// </summary>
[Serializable, NetSerializable]
public sealed class PlayRadioReceiveSoundEvent : EntityEventArgs
{
    /// <summary>
    /// Resolved channel sound to play for this receive notification.
    /// </summary>
    public SoundSpecifier Sound = default!;
}
