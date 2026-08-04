// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Numerics;
using Content.Shared.Paper;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Maths;
using Robust.Shared.Timing;

namespace Content.Client.Paper.UI;

public sealed partial class PaperWindow
{
    private const int InsertHelperPanelWidth = 170;
    private const int InsertHelperToggleSize = 24;
    private const int InsertHelperGap = 2;
    private const int InsertHelperPadding = 6;
    private const int ManifestOptionsMaxHeight = 220;
    private const int ManifestClosedTextMaxLength = 24;

    /// <summary>
    /// Prevents invalid time/date data from crashing the UI.
    /// The server sends the values, but the client still clamps unsafe deltas before formatting.
    /// </summary>
    private static readonly TimeSpan MaxClockAdvance = TimeSpan.FromDays(30);

    private PaperComponent.PaperInsertDataMessage? _insertData;
    private Popup _insertHelperPopup = default!;
    private PanelContainer _insertHelperPanel = default!;
    private Button _insertHelperToggleButton = default!;
    private Button _insertStationButton = default!;
    private Button _insertTimeDateButton = default!;
    private Button _insertOwnNameButton = default!;
    private Button _insertOwnJobButton = default!;
    private OptionButton _manifestOptionButton = default!;
    private Button _insertManifestNameButton = default!;
    private Button _insertManifestJobButton = default!;
    private Label? _manifestOptionLabel;
    private bool _insertHelperExpanded;

    [Dependency] private IGameTiming _gameTiming = default!;

    /// <summary>
    /// Builds the insert helper outside the paper layout.
    /// This keeps the paper size and resize behavior unchanged.
    /// </summary>
    private void InitializeInsertHelper()
    {
        _insertHelperToggleButton = new Button
        {
            Text = "▶",
            ToggleMode = true,
            SetWidth = InsertHelperToggleSize,
            SetHeight = InsertHelperToggleSize,
            Visible = false,
            StyleBoxOverride = new StyleBoxFlat(new Color(0f, 0f, 0f, 0.5f)),
            StyleClasses = { "OpenBoth" }
        };

        // Create all insert buttons disabled; server data enables them later.
        _insertStationButton = CreateInsertButton("paper-insert-helper-station");
        _insertTimeDateButton = CreateInsertButton("paper-insert-helper-time-date");
        _insertOwnNameButton = CreateInsertButton("paper-insert-helper-own-name");
        _insertOwnJobButton = CreateInsertButton("paper-insert-helper-own-job");
        _insertManifestNameButton = CreateInsertButton("paper-insert-helper-manifest-name");
        _insertManifestJobButton = CreateInsertButton("paper-insert-helper-manifest-job");

        // Manifest entries are selected through a compact dropdown.
        _manifestOptionButton = new OptionButton
        {
            HorizontalExpand = true,
            Disabled = true,
            StyleClasses = { "OpenBoth" }
        };
        _manifestOptionButton.OptionsScroll.MaxHeight = ManifestOptionsMaxHeight;
        _manifestOptionButton.OptionStyleClasses.Add("OpenBoth");

        // The closed _manifestOptionButton text is left-aligned so long names are truncated from the end. Popup options keep their full text.
        _manifestOptionLabel = FindFirstLabel(_manifestOptionButton);
        if (_manifestOptionLabel != null)
        {
            _manifestOptionLabel.RemoveStyleClass(OptionButton.StyleClassOptionButton);
            _manifestOptionLabel.Align = Label.AlignMode.Left;
            _manifestOptionLabel.ClipText = true;
        }

        // Build the popup panel separately from the main paper window.
        _insertHelperPanel = BuildInsertHelperPanel();
        _insertHelperPopup = new Popup { CloseOnClick = false };
        _insertHelperPopup.AddChild(_insertHelperPanel);
        _insertHelperPopup.OnPopupHide += OnInsertHelperPopupHide;

        // Toggle expands or collapses the helper popup.
        _insertHelperToggleButton.OnToggled += args => SetInsertHelperExpanded(args.Pressed);

        // Insert station name, or the PDA-style "Unknown" fallback.
        _insertStationButton.OnPressed += _ =>
            InsertHelperText(_insertData?.StationName ?? Loc.GetString("comp-pda-ui-unknown"));

        // Time/date is formatted client-side from the server snapshot.
        _insertTimeDateButton.OnPressed += _ =>
        {
            if (_insertData != null)
                InsertHelperText(FormatInsertHelperTimeDate(_insertData));
        };
        // Self data comes from the current character.
        _insertOwnNameButton.OnPressed += _ => InsertHelperText(_insertData?.OwnName);
        _insertOwnJobButton.OnPressed += _ => InsertHelperText(_insertData?.OwnJob);

        // Keep selected manifest ID in sync with the visible dropdown text.
        _manifestOptionButton.OnItemSelected += args => args.Button.SelectId(args.Id);
        _manifestOptionButton.OnItemSelected += _ => UpdateManifestOptionDisplayText();

        // Manifest name and job are inserted separately.
        _insertManifestNameButton.OnPressed += _ => InsertHelperText(GetSelectedManifestEntry()?.Name);
        _insertManifestJobButton.OnPressed += _ => InsertHelperText(GetSelectedManifestEntry()?.JobTitle);

        OnClose += OnPaperWindowClosed;
        ClearInsertData();
    }

    // Keep floating helper controls anchored while the paper window moves or resizes.
    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        if (_insertHelperToggleButton.Visible && _insertHelperToggleButton.Parent != null)
            UpdateInsertHelperTogglePosition();

        if (_insertHelperExpanded && _insertHelperPopup.Visible)
            UpdateInsertHelperPopupPosition();
    }

    public override void Close()
    {
        CloseInsertHelperOverlay();
        base.Close();
    }

    protected override void ExitedTree()
    {
        CloseInsertHelperOverlay();
        base.ExitedTree();
    }

    // The helper controls live in ModalRoot, not inside this window.
    // Dispose can bypass normal Close paths, so detach the overlay here as a final cleanup guard.
    [Obsolete("Controls should only be removed from UI tree instead of being disposed")]
    protected override void Dispose(bool disposing)
    {
        if (disposing)
            CloseInsertHelperOverlay();

        base.Dispose(disposing);
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

    public void UpdateInsertData(PaperComponent.PaperInsertDataMessage data)
    {
        _insertData = data;

        _insertStationButton.Disabled = false;
        _insertTimeDateButton.Disabled = false;
        _insertOwnNameButton.Disabled = string.IsNullOrWhiteSpace(data.OwnName);
        _insertOwnJobButton.Disabled = string.IsNullOrWhiteSpace(data.OwnJob);

        _manifestOptionButton.Clear();

        if (data.ManifestEntries.Length == 0)
        {
            AddManifestPlaceholder();
            _manifestOptionButton.Disabled = true;
            _insertManifestNameButton.Disabled = true;
            _insertManifestJobButton.Disabled = true;
            return;
        }

        for (var i = 0; i < data.ManifestEntries.Length; i++)
        {
            var entry = data.ManifestEntries[i];
            _manifestOptionButton.AddItem($"{entry.Name} - {entry.JobTitle}", i);
        }

        _manifestOptionButton.SelectId(0);
        UpdateManifestOptionDisplayText();
        _manifestOptionButton.Disabled = false;
        _insertManifestNameButton.Disabled = false;
        _insertManifestJobButton.Disabled = false;
    }

    private PanelContainer BuildInsertHelperPanel()
    {
        var box = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            Margin = new Thickness(InsertHelperPadding)
        };

        box.AddChild(new Label
        {
            Text = Loc.GetString("paper-insert-helper-title"),
            StyleClasses = { "LabelSecondaryColor" }
        });
        box.AddChild(_insertStationButton);
        box.AddChild(_insertTimeDateButton);
        box.AddChild(_insertOwnNameButton);
        box.AddChild(_insertOwnJobButton);
        box.AddChild(new Label
        {
            Text = Loc.GetString("paper-insert-helper-manifest"),
            Margin = new Thickness(0, 8, 0, 0),
            StyleClasses = { "LabelSecondaryColor" }
        });
        box.AddChild(_manifestOptionButton);

        var manifestButtons = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            HorizontalExpand = true
        };
        manifestButtons.AddChild(_insertManifestNameButton);
        manifestButtons.AddChild(_insertManifestJobButton);
        box.AddChild(manifestButtons);

        return new PanelContainer
        {
            SetWidth = InsertHelperPanelWidth,
            StyleClasses = { "TransparentBorderedWindowPanel" },
            Children = { box }
        };
    }

    private static Button CreateInsertButton(string locId)
    {
        return new Button
        {
            Text = Loc.GetString(locId),
            HorizontalExpand = true,
            Disabled = true,
            StyleClasses = { "OpenBoth" }
        };
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
    }

    /// <summary>
    /// Removes the floating helper controls from ModalRoot.
    /// They are outside the paper window, so they must be detached manually.
    /// </summary>
    public void CloseInsertHelperOverlay()
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
        {
            OpenInsertHelperPopup();
        }
        else
        {
            _insertHelperPopup.Close();
        }
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
        _insertHelperToggleButton.Measure(Vector2Helpers.Infinity);
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

    private void OnPaperWindowClosed()
    {
        CloseInsertHelperOverlay();
    }

    private void ClearInsertData()
    {
        _insertData = null;
        _insertStationButton.Disabled = true;
        _insertTimeDateButton.Disabled = true;
        _insertOwnNameButton.Disabled = true;
        _insertOwnJobButton.Disabled = true;
        _manifestOptionButton.Clear();
        AddManifestPlaceholder();
        _manifestOptionButton.Disabled = true;
        _insertManifestNameButton.Disabled = true;
        _insertManifestJobButton.Disabled = true;
    }

    private void AddManifestPlaceholder()
    {
        _manifestOptionButton.AddItem(Loc.GetString("paper-insert-helper-manifest-placeholder"), 0);
        UpdateManifestOptionDisplayText();
    }

    private void UpdateManifestOptionDisplayText()
    {
        if (_manifestOptionLabel == null)
            return;

        var text = Loc.GetString("paper-insert-helper-manifest-placeholder");
        if (_insertData != null &&
            _insertData.ManifestEntries.Length > 0 &&
            _manifestOptionButton.SelectedId >= 0 &&
            _manifestOptionButton.SelectedId < _insertData.ManifestEntries.Length)
        {
            var entry = _insertData.ManifestEntries[_manifestOptionButton.SelectedId];
            text = $"{entry.Name} - {entry.JobTitle}";
        }

        _manifestOptionLabel.Text = EllipsizeText(text, ManifestClosedTextMaxLength);
    }

    private static string EllipsizeText(string text, int maxLength)
    {
        if (text.Length <= maxLength)
            return text;

        if (maxLength <= 3)
            return new string('.', maxLength);

        return text[..(maxLength - 3)].TrimEnd() + "...";
    }

    private static Label? FindFirstLabel(Control control)
    {
        foreach (var child in control.Children)
        {
            if (child is Label label)
                return label;

            var nested = FindFirstLabel(child);
            if (nested != null)
                return nested;
        }

        return null;
    }

    private PaperComponent.PaperInsertManifestEntry? GetSelectedManifestEntry()
    {
        if (_insertData == null ||
            _insertData.ManifestEntries.Length == 0 ||
            _manifestOptionButton.SelectedId < 0 ||
            _manifestOptionButton.SelectedId >= _insertData.ManifestEntries.Length)
        {
            return null;
        }

        return _insertData.ManifestEntries[_manifestOptionButton.SelectedId];
    }

    private void InsertHelperText(string? text)
    {
        if (string.IsNullOrEmpty(text))
            return;

        Input.InsertAtCursor(text);
        Input.GrabKeyboardFocus();
        UpdateFillState();
    }

    private string FormatInsertHelperTimeDate(PaperComponent.PaperInsertDataMessage data)
    {
        var elapsed = _gameTiming.CurTime - data.GameTime;
        if (elapsed < TimeSpan.Zero || elapsed > MaxClockAdvance)
            elapsed = TimeSpan.Zero;

        var shiftTime = data.ShiftTime + elapsed;
        var serverDate = GetSafeServerDate(data).Add(elapsed);

        return $"{shiftTime:hh\\:mm\\:ss}, {serverDate.Day:00}.{serverDate.Month:00}.{serverDate.Year + 1000:0000}";
    }

    private static DateTime GetSafeServerDate(PaperComponent.PaperInsertDataMessage data)
    {
        // The server sends valid values, but clamping here keeps the UI resilient to old servers,
        // corrupted packets, or test harnesses that construct messages manually.
        var year = Math.Clamp(data.ServerYear, 1, 8999);
        var month = Math.Clamp(data.ServerMonth, 1, 12);
        var day = Math.Clamp(data.ServerDay, 1, DateTime.DaysInMonth(year, month));

        return new DateTime(year, month, day);
    }
}
