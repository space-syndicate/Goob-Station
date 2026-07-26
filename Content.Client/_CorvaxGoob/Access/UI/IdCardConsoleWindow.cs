// SPDX-License-Identifier: AGPL-3.0-or-later

using static Content.Shared.Access.Components.IdCardConsoleComponent;

namespace Content.Client.Access.UI
{
    public sealed partial class IdCardConsoleWindow
    {
        private bool _pendingExtendedAccessJobTitleSync;

        /// <summary>
        /// Connects the isolated extended-access buttons (Standard and Extended) to their server actions.
        /// </summary>
        private void InitializeExtendedAccessButtons()
        {
            StandardAccessButton.OnPressed += _ => SubmitExtendedAccessAction(IdCardConsoleExtendedAccessAction.StandardAccess);
            ExtendedAccessButton.OnPressed += _ => SubmitExtendedAccessAction(IdCardConsoleExtendedAccessAction.Extended);
        }

        /// <summary>
        /// Marks that an extended-access response is pending, so the next UpdateState can resync JobTitleLineEdit from the ID card.
        /// </summary>
        private void SubmitExtendedAccessAction(IdCardConsoleExtendedAccessAction action)
        {
            _pendingExtendedAccessJobTitleSync = true;
            _owner.SubmitExtendedAccessAction(action);
        }

        /// <summary>
        /// After an extended-access action changes the card title, replaces any unsaved JobTitleLineEdit text with the target ID card title.
        /// </summary>
        private void SyncJobTitleAfterExtendedAccess(string targetJobTitle)
        {
            if (!_pendingExtendedAccessJobTitleSync)
                return;

            JobTitleLineEdit.Text = targetJobTitle;
            _pendingExtendedAccessJobTitleSync = false;
        }

        private void SetExtendedAccessButtonsDisabled(bool disabled)
        {
            StandardAccessButton.Disabled = disabled;
            ExtendedAccessButton.Disabled = disabled;
        }
    }
}
