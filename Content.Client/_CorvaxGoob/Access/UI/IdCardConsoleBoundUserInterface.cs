// SPDX-License-Identifier: AGPL-3.0-or-later

using static Content.Shared.Access.Components.IdCardConsoleComponent;

namespace Content.Client.Access.UI
{
    public sealed partial class IdCardConsoleBoundUserInterface
    {
        /// <summary>
        /// Sends the isolated extended-access action message without touching the normal ID card write path.
        /// </summary>
        public void SubmitExtendedAccessAction(IdCardConsoleExtendedAccessAction action)
        {
            SendMessage(new IdCardConsoleExtendedAccessMessage(action));
        }
    }
}
