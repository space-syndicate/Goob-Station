// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Server.Ghost.Components
{
    /// <summary>
    /// Explicit state for the cyclic observer HUD action.
    /// This avoids inferring the current mode from overlay components, which makes future modes easier to add.
    /// </summary>
    public enum GhostObserverHudMode : byte
    {
        Off,
        Security,
        SecurityHealth,
    }

    /// <summary>
    /// Stores the current observer HUD mode for regular ghosts.
    /// The actual HUD components are applied by <see cref="GhostSystem"/> from this state.
    /// </summary>
    [RegisterComponent]
    public sealed partial class GhostObserverHudComponent : Component
    {
        public GhostObserverHudMode Mode = GhostObserverHudMode.Off;
    }
}
