// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Numerics;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Maths;
using Robust.Shared.Timing;

namespace Content.Client.Paper.UI;

/// <summary>
/// Manages the insert helper position and visibility.
/// </summary>
public sealed partial class PaperWindow
{
    private const int InsertHelperGap = 2;

    private Vector2? _lastInsertHelperPaperPosition;
    private Vector2? _lastInsertHelperPaperSize;

    // Keep floating helper controls anchored while the paper window moves or resizes.
    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        if (!_insertHelperToggleButton.Visible || _insertHelperToggleButton.Parent == null)
            return;

        var paperPosition = PaperBackground.GlobalPosition;
        var paperSize = PaperBackground.Size;
        if (_lastInsertHelperPaperPosition == paperPosition &&
            _lastInsertHelperPaperSize == paperSize)
        {
            return;
        }

        _lastInsertHelperPaperPosition = paperPosition;
        _lastInsertHelperPaperSize = paperSize;
        UpdateInsertHelperTogglePosition();

        if (_insertHelperExpanded && _insertHelperPopup.Visible)
            UpdateInsertHelperPopupPosition();
    }

    protected override void ExitedTree()
    {
        CloseInsertHelperOverlay();
        base.ExitedTree();
    }

    /// <summary>
    /// Called from the base Populate path whenever the paper changes between read/write modes.
    /// The helper is only available in write mode and is reset closed each time writing starts.
    /// </summary>
    private void SetInsertHelperEditingMode(bool isEditing, bool wasEditing)
    {
        if (!isEditing)
        {
            ClearInsertData();
            CloseInsertHelperOverlay();
            return;
        }

        AttachInsertHelperToggle();
        UpdateInsertHelperTogglePosition();

        if (!wasEditing)
            SetInsertHelperExpanded(false);
    }

    private void AttachInsertHelperToggle()
    {
        if (_insertHelperToggleButton.Parent == null)
            UserInterfaceManager.ModalRoot.AddChild(_insertHelperToggleButton);

        _insertHelperToggleButton.Visible = true;
    }

    private void DetachInsertHelperToggle()
    {
        _insertHelperToggleButton.Pressed = false;
        _insertHelperToggleButton.Visible = false;
        _insertHelperToggleButton.Orphan();
        _lastInsertHelperPaperPosition = null;
        _lastInsertHelperPaperSize = null;
    }

    /// <summary>
    /// Removes the floating helper controls from ModalRoot.
    /// They are outside the paper window, so they must be detached manually.
    /// </summary>
    internal void CloseInsertHelperOverlay()
    {
        _insertHelperExpanded = false;

        if (_insertHelperToggleButton != null)
            DetachInsertHelperToggle();

        if (_insertHelperPopup != null)
        {
            _insertHelperPopup.Close();
            _insertHelperPopup.Orphan();
        }
    }

    private void SetInsertHelperExpanded(bool expanded)
    {
        expanded &= _insertHelperToggleButton.Visible;
        _insertHelperExpanded = expanded;
        _insertHelperToggleButton.Pressed = expanded;

        if (expanded)
            OpenInsertHelperPopup();
        else
            _insertHelperPopup.Close();
    }

    private void OpenInsertHelperPopup()
    {
        if (_insertHelperPopup.Parent == null)
            UserInterfaceManager.ModalRoot.AddChild(_insertHelperPopup);

        _insertHelperPopup.Open(GetInsertHelperPopupBox());
    }

    private void UpdateInsertHelperPopupPosition()
    {
        PopupContainer.SetPopupOrigin(_insertHelperPopup, GetInsertHelperPopupBox().TopLeft);
    }

    private UIBox2 GetInsertHelperPopupBox()
    {
        _insertHelperPanel.Measure(Vector2Helpers.Infinity);
        _insertHelperToggleButton.Measure(Vector2Helpers.Infinity);

        var panelSize = _insertHelperPanel.DesiredSize;
        var toggleWidth = Math.Max(_insertHelperToggleButton.Width, _insertHelperToggleButton.DesiredSize.X);
        var togglePosition = _insertHelperToggleButton.GlobalPosition;
        var position = new Vector2(togglePosition.X + toggleWidth + InsertHelperGap, togglePosition.Y);

        return UIBox2.FromDimensions(position, panelSize);
    }

    private void UpdateInsertHelperTogglePosition()
    {
        PopupContainer.SetPopupOrigin(_insertHelperToggleButton, GetInsertHelperTogglePosition());
    }

    private Vector2 GetInsertHelperTogglePosition()
    {
        var paperPosition = PaperBackground.GlobalPosition;

        return new Vector2(
            paperPosition.X + PaperBackground.Width + InsertHelperGap,
            paperPosition.Y);
    }

    private void OnInsertHelperPopupHide()
    {
        _insertHelperExpanded = false;
        _insertHelperToggleButton.Pressed = false;
        _insertHelperPopup.Orphan();
    }
}
