// SPDX-License-Identifier: AGPL-3.0-or-later

using static Content.Shared.Access.Components.IdCardConsoleComponent;

namespace Content.Client.Access.UI
{
    public sealed partial class IdCardConsoleWindow
    {
        private bool _pendingAccessActionJobTitleSync;

        /// <summary>
        /// Connects the console's standard grant-all/revoke-all buttons to job-title marker actions.
        /// </summary>
        private void InitializeStandardAllAccessButtons()
        {
            SelectAllButton.OnPressed += _ => SubmitStandardAllAccessAction(true, IdCardConsoleAccessMarkerAction.Add);
            DeselectAllButton.OnPressed += _ => SubmitStandardAllAccessAction(false, IdCardConsoleAccessMarkerAction.Remove);
        }

        /// <summary>
        /// Connects the bulk access buttons (Standard and Extended) to their server actions.
        /// </summary>
        private void InitializeBulkAccessButtons()
        {
            StandardAccessButton.OnPressed += _ => SubmitBulkAccessAction(IdCardConsoleBulkAccessAction.StandardAccess);
            ExtendedAccessButton.OnPressed += _ => SubmitBulkAccessAction(IdCardConsoleBulkAccessAction.Extended);
        }

        /// <summary>
        /// Marks that a server-side access action is pending, so the next UpdateState can resync JobTitleLineEdit from the ID card.
        /// </summary>
        private void SubmitBulkAccessAction(IdCardConsoleBulkAccessAction action)
        {
            _pendingAccessActionJobTitleSync = true;
            _owner.SubmitBulkAccessAction(action);
        }

        /// <summary>
        /// Marks all visible access buttons on or off through the normal write path, with a server-side request to update the visual "+" marker.
        /// </summary>
        private void SubmitStandardAllAccessAction(bool enabled, IdCardConsoleAccessMarkerAction accessMarkerAction)
        {
            SetAllAccess(enabled);
            _pendingAccessActionJobTitleSync = true;
            SubmitData(accessMarkerAction);
        }

        /// <summary>
        /// After a server-side access action changes the card title, replaces any unsaved JobTitleLineEdit text with the target ID card title.
        /// </summary>
        private void SyncJobTitleAfterAccessAction(string targetJobTitle)
        {
            if (!_pendingAccessActionJobTitleSync)
                return;

            JobTitleLineEdit.Text = targetJobTitle;
            _pendingAccessActionJobTitleSync = false;
        }

        private void SetBulkButtonsDisabled(bool disabled)
        {
            StandardAccessButton.Disabled = disabled;
            ExtendedAccessButton.Disabled = disabled;
        }
    }
}
