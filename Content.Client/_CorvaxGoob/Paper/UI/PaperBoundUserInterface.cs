// SPDX-License-Identifier: AGPL-3.0-or-later

using static Content.Shared.Paper.PaperComponent;

namespace Content.Client.Paper.UI;

public sealed partial class PaperBoundUserInterface
{
    private bool _insertDataRequested;

    /// <summary>
    /// Reset when a fresh BUI instance opens so every new paper editing session asks
    /// the server for an up-to-date PDA/station/manifest snapshot.
    /// </summary>
    private void ResetInsertDataRequest()
    {
        _insertDataRequested = false;
    }

    /// <summary>
    /// Requests insert-helper data once per write session.
    /// The server derives all values from the actor, so the client does not send entity or station IDs.
    /// </summary>
    private void UpdateInsertDataRequest(PaperBoundUserInterfaceState paperState)
    {
        if (paperState.Mode != PaperAction.Write)
        {
            _insertDataRequested = false;
            return;
        }

        if (_insertDataRequested)
            return;

        _insertDataRequested = true;
        SendMessage(new PaperInsertDataRequestMessage());
    }

    /// <summary>
    /// Handles the private server response for this viewer.
    /// These values are actor-specific, so they are not sent through shared BUI state.
    /// </summary>
    private void ReceiveInsertDataMessage(BoundUserInterfaceMessage message)
    {
        if (message is PaperInsertDataMessage insertData)
            _window?.UpdateInsertData(insertData);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            _window?.CloseInsertHelperOverlay();

        base.Dispose(disposing);
    }
}
