// SPDX-License-Identifier: AGPL-3.0-or-later

using static Content.Shared.Access.Components.IdCardConsoleComponent;

namespace Content.Client.Access.UI
{
    public sealed partial class IdCardConsoleBoundUserInterface
    {
        public void SubmitExtendedAccessAction(IdCardConsoleExtendedAccessAction action)
        {
            SendMessage(new IdCardConsoleExtendedAccessMessage(action));
        }
    }
}
