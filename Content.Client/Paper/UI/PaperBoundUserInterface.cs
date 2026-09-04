// SPDX-License-Identifier: AGPL-3.0-or-later

using JetBrains.Annotations;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Utility;
using Content.Shared.Paper;
using static Content.Shared.Paper.PaperComponent;

namespace Content.Client.Paper.UI;

[UsedImplicitly]
public sealed partial class PaperBoundUserInterface : BoundUserInterface // CorvaxGoob Edit - made partial
{
    [ViewVariables]
    private PaperWindow? _window;

    public PaperBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();
        ResetInsertDataRequest(); // CorvaxGoob - Edit-documents-helper

        _window = this.CreateWindow<PaperWindow>();
        _window.OnSaved += InputOnTextEntered;
        _window.OnSignatureRequested += OnSignatureRequested; // Starlight-edit

        if (EntMan.TryGetComponent<PaperComponent>(Owner, out var paper))
        {
            _window.MaxInputLength = paper.ContentSize;
        }
        if (EntMan.TryGetComponent<PaperVisualsComponent>(Owner, out var visuals))
        {
            _window.InitVisuals(Owner, visuals);
        }
    }

    // CorvaxGoob Edit Start - Edit-documents-helper
    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);
        var paperState = (PaperBoundUserInterfaceState) state;
        _window?.Populate(paperState);
        UpdateInsertDataRequest(paperState);
    }

    protected override void ReceiveMessage(BoundUserInterfaceMessage message)
    {
        base.ReceiveMessage(message);
        ReceiveInsertDataMessage(message);
    }
    // CorvaxGoob End

    private void InputOnTextEntered(string text)
    {
        SendMessage(new PaperInputTextMessage(text));

        if (_window != null)
        {
            _window.Input.TextRope = Rope.Leaf.Empty;
            _window.Input.CursorPosition = new TextEdit.CursorPos(0, TextEdit.LineBreakBias.Top);
        }
    }

    // Starlight
    private void OnSignatureRequested(int signatureIndex) => SendMessage(new PaperSignatureRequestMessage(signatureIndex));
}
