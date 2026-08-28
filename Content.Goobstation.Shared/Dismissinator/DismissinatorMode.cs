// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Serialization;

namespace Content.Goobstation.Shared.Dismissinator;

/// <summary>
///     Which way the paperwork runs.
/// </summary>
[Serializable, NetSerializable]
public enum DismissinatorMode : byte
{
    /// <summary>
    ///     Strips the target's card and serves a dismissal notice.
    /// </summary>
    Dismissal,

    /// <summary>
    ///     Signs over the authorizing card's own clearance and serves an access expansion notice.
    /// </summary>
    Expansion,

    /// <summary>
    ///     Emag-only. Saddles the target's mind with a syndicate objective and serves it as a directive.
    /// </summary>
    Objective,
}
