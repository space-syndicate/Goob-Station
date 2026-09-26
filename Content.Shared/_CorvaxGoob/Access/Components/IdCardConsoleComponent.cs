// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Serialization;

namespace Content.Shared.Access.Components;

public sealed partial class IdCardConsoleComponent
{
    /// <summary>
    /// UI message for an extended-access action in the ID card console.
    /// </summary>
    /// <remarks>
    /// Kept separate from the normal write message so the feature does not affect existing access editing.
    /// </remarks>
    [Serializable, NetSerializable]
    public sealed class IdCardConsoleExtendedAccessMessage : BoundUserInterfaceMessage
    {
        public readonly IdCardConsoleExtendedAccessAction Action;

        public IdCardConsoleExtendedAccessMessage(IdCardConsoleExtendedAccessAction action)
        {
            Action = action;
        }
    }

    /// <summary>
    /// Extended-access actions for the ID card console.
    /// </summary>
    /// <remarks>
    /// Used by the client and server to identify which isolated extended-access operation was selected.
    /// </remarks>
    [Serializable, NetSerializable]
    public enum IdCardConsoleExtendedAccessAction : byte
    {
        StandardAccess,
        Extended,
    }
}
