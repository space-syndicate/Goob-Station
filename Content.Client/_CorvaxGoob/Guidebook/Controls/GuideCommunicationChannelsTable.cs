// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Client.Guidebook.Controls;
using Content.Client.Guidebook.Richtext;
using Content.Client.Message;
using Content.Client.UserInterface.ControlExtensions;
using Content.Shared._Starlight.CollectiveMind;
using Content.Shared.Chat;
using Content.Shared.Radio;
using JetBrains.Annotations;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Client._CorvaxGoob.Guidebook.Controls;

/// <summary>
/// Builds the localized communication-channel reference table used by the guidebook.
/// Radio and collective-mind prototypes are read at runtime, so newly added channels
/// appear here without maintaining a second hard-coded list.
/// </summary>
[UsedImplicitly]
public sealed class GuideCommunicationChannelsTable : BoxContainer, IDocumentTag
{
    [Dependency] private IPrototypeManager _prototype = default!;
    [Dependency] private IClipboardManager _clipboard = default!;

    private const int NameWidth = 190;
    private const int KeyWidth = 80;
    private const string LatinKeyColor = "#8fcfff";
    private const string GuideHintColor = "#c8a16c";

    // Rows are retained so the local search field can hide them without rebuilding the table.
    private readonly List<CommunicationChannelGuideRow> _rows = [];

    public GuideCommunicationChannelsTable()
    {
        IoCManager.InjectDependencies(this);

        Orientation = LayoutOrientation.Vertical;
        HorizontalExpand = true;
        MouseFilter = MouseFilterMode.Stop;
    }

    public bool TryParseTag(Dictionary<string, string> args, [NotNullWhen(true)] out Control? control)
    {
        GenerateTable();
        control = this;
        return true;
    }

    private void GenerateTable()
    {
        RemoveAllChildren();
        _rows.Clear();

        // Page copy is built here instead of being hard-coded in the XML document,
        // allowing the same guide entry to work with every available locale.
        AddChild(BuildPageTitle());
        AddChild(BuildIntroduction());
        AddChild(BuildCopyHint());
        AddChild(BuildSearchBar());
        AddChild(BuildHeaderRow());

        var rows = _prototype.EnumeratePrototypes<RadioChannelPrototype>()
            .Select(BuildRadioRow)
            .Concat(_prototype.EnumeratePrototypes<CollectiveMindPrototype>().Select(BuildCollectiveMindRow))
            // Sort by the already localized channel name. Keep this simple because
            // client content is sandbox-checked and some runtime comparer types are blocked.
            .OrderBy(row => row.Name)
            .ToList();

        foreach (var row in rows)
        {
            var control = new CommunicationChannelGuideRow(row, _clipboard);
            _rows.Add(control);
            AddChild(control);
        }
    }

    private static Label BuildPageTitle()
    {
        return new Label
        {
            Text = Loc.GetString("guide-communication-channels-page-title"),
            StyleClasses = { "LabelHeadingBigger" }
        };
    }

    private static RichTextLabel BuildIntroduction()
    {
        var introduction = new RichTextLabel
        {
            HorizontalExpand = true,
            Margin = new Thickness(0, 2, 0, 2)
        };

        introduction.SetMarkup(Loc.GetString(
            "guide-communication-channels-page-introduction",
            ("keyColor", GuideHintColor)));
        return introduction;
    }

    private static RichTextLabel BuildCopyHint()
    {
        var hint = new RichTextLabel
        {
            HorizontalExpand = true
        };

        hint.SetMarkup(Loc.GetString(
            "guide-communication-channels-page-copy-hint",
            ("keyColor", GuideHintColor)));
        return hint;
    }

    private LineEdit BuildSearchBar()
    {
        var search = new LineEdit
        {
            PlaceHolder = Loc.GetString("guide-communication-channels-search-placeholder"),
            HorizontalExpand = true,
            Margin = new Thickness(0, 4, 0, 4)
        };

        search.OnTextChanged += _ => ApplyFilter(search.Text);
        return search;
    }

    private void ApplyFilter(string query)
    {
        foreach (var row in _rows)
        {
            row.SetHiddenState(true, query);
        }
    }

    private static CommunicationChannelGuideData BuildRadioRow(RadioChannelPrototype channel)
    {
        // Common radio uses ';'. Channels without a key (such as Handheld) intentionally
        // leave the table cell empty because there is no prefix the player can type.
        var prefix = channel.ID == SharedChatSystem.CommonChannel
            ? SharedChatSystem.RadioCommonPrefix.ToString()
            : channel.KeyCode == '\0'
                ? string.Empty
                : $"{SharedChatSystem.RadioChannelPrefix}{char.ToLowerInvariant(channel.KeyCode)}";

        return new CommunicationChannelGuideData(
            channel.LocalizedName,
            prefix,
            GetDescription("radio", channel.ID, channel.LocalizedName),
            channel.Color);
    }

    private static CommunicationChannelGuideData BuildCollectiveMindRow(CollectiveMindPrototype mind)
    {
        var prefix = mind.KeyCode == '\0'
            ? string.Empty
            : $"{SharedChatSystem.CollectiveMindPrefix}{char.ToLowerInvariant(mind.KeyCode)}";

        return new CommunicationChannelGuideData(
            mind.LocalizedName,
            prefix,
            GetDescription("collective-mind", mind.ID, mind.LocalizedName),
            mind.Color);
    }

    private static string GetDescription(string kind, string id, string name)
    {
        // A channel-specific description is preferred, while the generic text keeps
        // mod-added prototypes useful even before a dedicated localization is written.
        var specificKey = $"guide-communication-channels-description-{kind}-{id.ToLowerInvariant()}";
        if (Loc.TryGetString(specificKey, out var description))
            return description;

        return Loc.GetString($"guide-communication-channels-description-{kind}-generic", ("channel", name));
    }

    private static Control BuildHeaderRow()
    {
        var panel = new PanelContainer
        {
            HorizontalExpand = true,
            Margin = new Thickness(0, 2, 0, 2),
            PanelOverride = new StyleBoxFlat
            {
                BackgroundColor = Color.FromHex("#252735"),
                BorderColor = Color.FromHex("#4c5066"),
                BorderThickness = new Thickness(1)
            }
        };

        var row = new BoxContainer
        {
            Orientation = LayoutOrientation.Horizontal,
            HorizontalExpand = true,
            Margin = new Thickness(6, 4)
        };

        row.AddChild(BuildHeaderLabel("guide-communication-channels-header-name", NameWidth));
        row.AddChild(BuildCenteredHeaderLabel("guide-communication-channels-header-key"));
        row.AddChild(BuildHeaderLabel("guide-communication-channels-header-description", 0, true));

        panel.AddChild(row);
        return panel;
    }

    private static RichTextLabel BuildHeaderLabel(string locKey, int width, bool expand = false)
    {
        var label = new RichTextLabel
        {
            HorizontalExpand = expand
        };

        if (!expand)
            label.SetWidth = width;

        label.SetMarkup($"[bold]{FormattedMessage.EscapeText(Loc.GetString(locKey))}[/bold]");
        return label;
    }

    private static Control BuildCenteredHeaderLabel(string locKey)
    {
        var text = FormattedMessage.EscapeText(Loc.GetString(locKey));
        return BuildKeyCell($"[bold]{text}[/bold]");
    }

    /// <summary>
    /// Creates the fixed-width key cell. Centering a RichTextLabel itself does not
    /// center its markup, so a centered child is placed inside this container.
    /// </summary>
    private static BoxContainer BuildKeyCell(string? markup = null, string? copyText = null, IClipboardManager? clipboard = null)
    {
        var cell = new BoxContainer
        {
            Orientation = LayoutOrientation.Horizontal,
            Align = BoxContainer.AlignMode.Center,
            SetWidth = KeyWidth
        };

        if (markup == null)
            return cell;

        var label = new RichTextLabel
        {
            HorizontalAlignment = HAlignment.Center
        };

        label.SetMarkup(markup);

        if (copyText != null && clipboard != null)
        {
            var button = new ContainerButton
            {
                HorizontalAlignment = HAlignment.Center
            };

            button.OnPressed += _ => clipboard.SetText(copyText);
            button.AddChild(label);
            cell.AddChild(button);
            return cell;
        }

        cell.AddChild(label);
        return cell;
    }

    private sealed class CommunicationChannelGuideRow : PanelContainer, ISearchableControl
    {
        private readonly IClipboardManager _clipboard;
        private readonly string _searchText;

        public CommunicationChannelGuideRow(CommunicationChannelGuideData data, IClipboardManager clipboard)
        {
            _clipboard = clipboard;
            HorizontalExpand = true;
            Margin = new Thickness(0, 0, 0, 2);
            PanelOverride = new StyleBoxFlat
            {
                BackgroundColor = Color.FromHex("#1d1f2a"),
                BorderColor = Color.FromHex("#393c4d"),
                BorderThickness = new Thickness(1)
            };

            var row = new BoxContainer
            {
                Orientation = LayoutOrientation.Horizontal,
                HorizontalExpand = true,
                Margin = new Thickness(6, 4)
            };

            row.AddChild(BuildChannelName(data.Name, data.Color));
            row.AddChild(BuildKey(data.Prefix));
            row.AddChild(BuildDescription(data.Description));

            AddChild(row);

            _searchText = $"{data.Prefix} {data.Name} {data.Description}";
        }

        public bool CheckMatchesSearch(string query)
        {
            return string.IsNullOrWhiteSpace(query)
                || _searchText.Contains(query.Trim(), StringComparison.OrdinalIgnoreCase)
                || this.ChildrenContainText(query);
        }

        public void SetHiddenState(bool state, string query)
        {
            Visible = CheckMatchesSearch(query) ? state : !state;
        }

        private static RichTextLabel BuildDescription(string text)
        {
            var label = new RichTextLabel
            {
                HorizontalExpand = true
            };

            label.SetMarkup(FormattedMessage.EscapeText(text));
            return label;
        }

        private Control BuildKey(string prefix)
        {
            if (string.IsNullOrEmpty(prefix))
                return BuildKeyCell();

            var escapedPrefix = FormattedMessage.EscapeText(prefix);
            string markup;

            // Highlight only Latin key letters, leaving ':' or '+' and every
            // non-Latin character in the normal text color.
            var hasSelectableKey = prefix.Length > 1 &&
                (prefix[0] == SharedChatSystem.RadioChannelPrefix ||
                 prefix[0] == SharedChatSystem.CollectiveMindPrefix);

            if (hasSelectableKey && IsLatinLetter(prefix[^1]))
            {
                var marker = FormattedMessage.EscapeText(prefix[..^1]);
                var key = FormattedMessage.EscapeText(prefix[^1].ToString());
                markup = $"[bold]{marker}[color={LatinKeyColor}]{key}[/color][/bold]";
            }
            else
            {
                markup = $"[bold]{escapedPrefix}[/bold]";
            }

            return BuildKeyCell(markup, prefix, _clipboard);
        }

        private static bool IsLatinLetter(char value)
        {
            return value is >= 'A' and <= 'Z' or >= 'a' and <= 'z';
        }

        private static RichTextLabel BuildChannelName(string name, Color color)
        {
            var label = new RichTextLabel
            {
                // A fixed width keeps all columns aligned and lets long localized
                // names such as «Центральное Командование» wrap onto two lines.
                SetWidth = NameWidth
            };

            label.SetMarkup($"[color={color.ToHex()}][bold]{FormattedMessage.EscapeText(name)}[/bold][/color]");
            return label;
        }
    }

    private sealed record CommunicationChannelGuideData(
        string Name,
        string Prefix,
        string Description,
        Color Color);
}
