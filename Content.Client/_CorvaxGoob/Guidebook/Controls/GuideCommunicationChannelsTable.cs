// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Client.Guidebook.Controls;
using Content.Client.Guidebook.Richtext;
using Content.Client.Message;
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
    private const string GuideHintColor = "#c8a16c";

    private static readonly StyleBoxFlat HeaderPanelStyle = new()
    {
        BackgroundColor = Color.FromHex("#252735"),
        BorderColor = Color.FromHex("#4c5066"),
        BorderThickness = new Thickness(1)
    };

    private static readonly StyleBoxFlat RowPanelStyle = new()
    {
        BackgroundColor = Color.FromHex("#1d1f2a"),
        BorderColor = Color.FromHex("#393c4d"),
        BorderThickness = new Thickness(1)
    };

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

        // Page layout: title, introduction, copy hint, filter, header, then generated rows.
        AddChild(BuildPageTitle());
        AddChild(BuildIntroduction());
        AddChild(BuildCopyHint());
        AddChild(BuildSearchBar());
        AddChild(BuildHeaderRow());

        // Sort by the already localized channel name.
        var rows = _prototype.EnumeratePrototypes<RadioChannelPrototype>()
            .Select(BuildRadioRow)
            .Concat(_prototype.EnumeratePrototypes<CollectiveMindPrototype>().Select(BuildCollectiveMindRow))
            .OrderBy(static row => row.Name);

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
            Text = Loc.GetString("guide-entry-communication-channels"),
            StyleClasses = { "LabelHeadingBigger" }
        };
    }

    private static RichTextLabel BuildIntroduction()
    {
        return BuildPageText(
            "guide-communication-channels-page-introduction",
            new Thickness(0, 2, 0, 2));
    }

    private static RichTextLabel BuildCopyHint()
    {
        return BuildPageText("guide-communication-channels-page-copy-hint");
    }

    // Creates localized rich-text blocks shown above the channel table.
    private static RichTextLabel BuildPageText(string locKey, Thickness margin = default)
    {
        var label = new RichTextLabel
        {
            HorizontalExpand = true,
            Margin = margin
        };

        label.SetMarkup(Loc.GetString(locKey, ("keyColor", GuideHintColor)));
        return label;
    }

    private LineEdit BuildSearchBar()
    {
        var search = new LineEdit
        {
            PlaceHolder = Loc.GetString("guide-communication-channels-search-placeholder"),
            HorizontalExpand = true,
            Margin = new Thickness(0, 4, 0, 4)
        };

        search.OnTextChanged += args => ApplyFilter(args.Text);
        return search;
    }

    private void ApplyFilter(string query)
    {
        foreach (var row in _rows)
        {
            row.SetHiddenState(true, query);
        }
    }

    // Builds a row from the localized name, keycode, and color of a radioChannel prototype.
    private static CommunicationChannelGuideData BuildRadioRow(RadioChannelPrototype channel)
    {
        // Common uses the chat-wide ';' prefix. Other channels combine the ':' prefix
        // with RadioChannelPrototype.KeyCode, which is loaded from the prototype's keycode field.
        var name = channel.LocalizedName;
        var prefix = channel.ID == SharedChatSystem.CommonChannel
            ? SharedChatSystem.RadioCommonPrefix.ToString()
            : BuildPrefix(SharedChatSystem.RadioChannelPrefix, channel.KeyCode);

        return new CommunicationChannelGuideData(
            name,
            prefix,
            GetDescription("radio", channel.ID, name),
            channel.Color);
    }

    // Builds a row from the localized name, keycode, and color of a collectiveMind prototype.
    private static CommunicationChannelGuideData BuildCollectiveMindRow(CollectiveMindPrototype mind)
    {
        // The '+' prefix comes from the chat system, while CollectiveMindPrototype.KeyCode
        // is loaded from the prototype's keycode field.
        var name = mind.LocalizedName;
        var prefix = BuildPrefix(SharedChatSystem.CollectiveMindPrefix, mind.KeyCode);

        return new CommunicationChannelGuideData(
            name,
            prefix,
            GetDescription("collective-mind", mind.ID, name),
            mind.Color);
    }

    // Returns the exact prefix players type in chat, or no key for keyless prototypes.
    private static string BuildPrefix(char prefix, char keyCode)
    {
        return keyCode == '\0'
            ? string.Empty
            : $"{prefix}{char.ToLowerInvariant(keyCode)}";
    }

    // Uses a specific description when present, otherwise falls back to generic text.
    private static string GetDescription(string kind, string id, string name)
    {
        var specificKey = $"guide-communication-channels-description-{kind}-{id.ToLowerInvariant()}";
        if (Loc.TryGetString(specificKey, out var description))
            return description;

        return Loc.GetString($"guide-communication-channels-description-{kind}-generic", ("channel", name));
    }

    private static PanelContainer BuildHeaderRow()
    {
        var panel = new PanelContainer
        {
            HorizontalExpand = true,
            Margin = new Thickness(0, 2, 0, 2),
            PanelOverride = HeaderPanelStyle
        };

        var row = new BoxContainer
        {
            Orientation = LayoutOrientation.Horizontal,
            HorizontalExpand = true,
            Margin = new Thickness(6, 4)
        };

        row.AddChild(BuildHeaderLabel("guide-communication-channels-header-name", NameWidth));
        row.AddChild(BuildCenteredHeaderLabel("guide-communication-channels-header-key"));
        row.AddChild(BuildHeaderLabel("guide-communication-channels-header-description"));

        panel.AddChild(row);
        return panel;
    }

    private static RichTextLabel BuildHeaderLabel(string locKey, int? width = null)
    {
        var label = new RichTextLabel
        {
            HorizontalExpand = width == null
        };

        if (width != null)
            label.SetWidth = width.Value;

        label.SetMarkup($"[bold]{FormattedMessage.EscapeText(Loc.GetString(locKey))}[/bold]");
        return label;
    }

    private static BoxContainer BuildCenteredHeaderLabel(string locKey)
    {
        var text = FormattedMessage.EscapeText(Loc.GetString(locKey));
        return BuildKeyCell($"[bold]{text}[/bold]");
    }

    /// <summary>
    /// Creates the fixed-width key cell. Centering a RichTextLabel itself does not
    /// center its markup, so a centered child is placed inside this container.
    /// </summary>
    private static BoxContainer BuildKeyCell(
        string? markup = null,
        string? copyText = null,
        IClipboardManager? clipboard = null)
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

    // Renders one channel row, stores searchable text for the filter,
    // and lets players copy a non-empty chat key by clicking it.
    private sealed class CommunicationChannelGuideRow : PanelContainer, ISearchableControl
    {
        private readonly string _searchText;

        public CommunicationChannelGuideRow(CommunicationChannelGuideData data, IClipboardManager clipboard)
        {
            HorizontalExpand = true;
            Margin = new Thickness(0, 0, 0, 2);
            PanelOverride = RowPanelStyle;

            var row = new BoxContainer
            {
                Orientation = LayoutOrientation.Horizontal,
                HorizontalExpand = true,
                Margin = new Thickness(6, 4)
            };

            row.AddChild(BuildChannelName(data.Name, data.Color));
            row.AddChild(BuildKey(data.Prefix, clipboard));
            row.AddChild(BuildDescription(data.Description));

            AddChild(row);

            _searchText = $"{data.Prefix} {data.Name} {data.Description}";
        }

        public bool CheckMatchesSearch(string query)
        {
            var search = query.Trim();
            return search.Length == 0
                || _searchText.Contains(search, StringComparison.OrdinalIgnoreCase);
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

        // Builds the key cell, adds color highlighting, and wires click-to-copy.
        private static BoxContainer BuildKey(string prefix, IClipboardManager clipboard)
        {
            if (string.IsNullOrEmpty(prefix))
                return BuildKeyCell();

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
                markup = $"[bold]{marker}[color={GuideHintColor}]{key}[/color][/bold]";
            }
            else
            {
                markup = $"[bold]{FormattedMessage.EscapeText(prefix)}[/bold]";
            }

            return BuildKeyCell(markup, prefix, clipboard);
        }

        private static bool IsLatinLetter(char value)
        {
            return value is >= 'A' and <= 'Z' or >= 'a' and <= 'z';
        }

        private static RichTextLabel BuildChannelName(string name, Color color)
        {
            var label = new RichTextLabel
            {
                // A fixed width keeps columns aligned and lets long names wrap.
                SetWidth = NameWidth
            };

            label.SetMarkup($"[color={color.ToHex()}][bold]{FormattedMessage.EscapeText(name)}[/bold][/color]");
            return label;
        }
    }

    // Prepared data used to render one channel table row.
    private sealed record CommunicationChannelGuideData(
        string Name,
        string Prefix,
        string Description,
        Color Color);
}
