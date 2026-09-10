// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Paper;
using Robust.Client.Graphics;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Maths;

namespace Content.Client.Paper.UI;

/// <summary>
/// Creates the paper insert helper controls.
/// </summary>
public sealed partial class PaperWindow
{
    private const int InsertHelperPanelWidth = 170;
    private const int InsertHelperToggleSize = 24;
    private const int InsertHelperPadding = 6;
    private const int ManifestOptionsMaxHeight = 220;

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

        // Keep the beginning of long names visible in the closed manifest button.
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

        ClearInsertData();
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
}
