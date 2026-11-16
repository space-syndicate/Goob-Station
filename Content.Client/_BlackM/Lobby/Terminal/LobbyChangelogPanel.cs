using System.Linq;
using System.Numerics;
using Content.Client.Changelog;
using Content.Client.Resources;
using Content.Client.Stylesheets;
using Content.Client.UserInterface.Controls;
using ChangelogData = Content.Client.Changelog.ChangelogManager.Changelog;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Utility;
using static Content.Client.Changelog.ChangelogManager;
using static Robust.Client.UserInterface.Controls.BoxContainer;

namespace Content.Client._BlackM.Lobby.Terminal;

public sealed class LobbyChangelogPanel : PanelContainer
{
    [Dependency] private readonly ChangelogManager _changelog = default!;
    [Dependency] private readonly IResourceCache _resCache = default!;
    private const string PreferredChangelog = "BlackM";

    private static readonly Color TextColor = Color.FromHex("#E9D8B4");
    private static readonly Color MutedColor = Color.FromHex("#A08050");
    private static readonly Color CardColor = Color.FromHex("#100C07A0");

    private readonly Label _title;
    private readonly ScrollContainer _scroll;
    private readonly BoxContainer _body;
    private readonly Label _versionLabel;

    private List<ChangelogData> _changelogs = new();
    private ChangelogData? _current;
    private List<ChangelogEntry> _filtered = new();
    private Dictionary<DateTime, int> _dayCounts = new();
    private DateTime? _lastDay;
    private bool _loaded;
    private bool _loading;

    public LobbyChangelogPanel()
    {
        IoCManager.InjectDependencies(this);

        PanelOverride = new StyleBoxFlat
        {
            BackgroundColor = Color.FromHex("#0B0804A0"),
        };

        var root = new BoxContainer
        {
            Orientation = LayoutOrientation.Vertical,
            SeparationOverride = 4,
            Margin = new Thickness(8),
        };
        AddChild(root);

        // Header
        _title = TermLabel(Loc.GetString("blackm-changelog-title"), 12);
        _title.HorizontalExpand = true;
        _title.VerticalAlignment = VAlignment.Center;

        root.AddChild(new BoxContainer
        {
            Orientation = LayoutOrientation.Horizontal,
            SeparationOverride = 6,
            Children = { _title },
        });
        root.AddChild(new HLine { Color = StyleNano.TerminalGreenDim.WithAlpha(0.3f), Thickness = 1 });

        // Body
        _body = new BoxContainer
        {
            Orientation = LayoutOrientation.Vertical,
            SeparationOverride = 4,
            HorizontalExpand = true,
        };
        _scroll = new ScrollContainer
        {
            VerticalExpand = true,
            HorizontalExpand = true,
            HScrollEnabled = false,
            Children = { _body },
        };
        root.AddChild(_scroll);

        // Footer
        root.AddChild(new Control { MinHeight = 2 });

        _versionLabel = TermLabel(string.Empty, 10, MutedColor);
        _versionLabel.HorizontalExpand = true;
        _versionLabel.Align = Label.AlignMode.Right;
        root.AddChild(_versionLabel);

        ShowMessage(Loc.GetString("blackm-changelog-loading"));
    }

    protected override void EnteredTree()
    {
        base.EnteredTree();

        if (!_loaded)
            Load();
    }

    private async void Load()
    {
        if (_loading)
            return;

        _loading = true;
        var changelogs = await _changelog.LoadChangelog();
        _loading = false;
        _loaded = true;

        _changelogs = changelogs;
        _versionLabel.Text = _changelog.GetClientVersion();
        RebuildTabs();
    }

    private void RebuildTabs()
    {
        _current = _changelogs.FirstOrDefault(c => c.Name == PreferredChangelog);
        Rebuild();
    }

    private void Rebuild()
    {
        _body.RemoveAllChildren();
        _lastDay = null;

        if (_current == null)
        {
            _filtered = new List<ChangelogEntry>();
            _dayCounts = new Dictionary<DateTime, int>();
            ShowMessage(Loc.GetString("blackm-changelog-empty"));
            return;
        }

        _filtered = _current.Entries
            .Where(e => e.Changes != null && e.Changes.Count > 0)
            .OrderByDescending(e => e.Time)
            .ThenByDescending(e => e.Id)
            .ToList();

        _dayCounts = _filtered
            .GroupBy(e => e.Time.ToLocalTime().Date)
            .ToDictionary(g => g.Key, g => g.Sum(e => e.Changes.Count));

        if (_filtered.Count == 0)
        {
            ShowMessage(Loc.GetString("blackm-changelog-empty"));
            return;
        }

        _scroll.SetScrollValue(default);
        foreach (var entry in _filtered)
        {
            var day = entry.Time.ToLocalTime().Date;

            if (_lastDay != day)
            {
                _lastDay = day;
                _body.AddChild(DayHeader(day));
            }

            _body.AddChild(EntryCard(entry));
        }

    }

    private Control DayHeader(DateTime day)
    {
        var today = DateTime.Today;
        string dayNice;
        if (day == today)
            dayNice = Loc.GetString("changelog-today");
        else if (day == today.AddDays(-1))
            dayNice = Loc.GetString("changelog-yesterday");
        else
            dayNice = day.ToString("dd.MM.yyyy");

        var name = TermLabel("> " + dayNice.ToUpperInvariant(), 11);
        var count = TermLabel(
            Loc.GetString("blackm-changelog-day-count", ("count", _dayCounts.GetValueOrDefault(day))),
            10,
            MutedColor);
        count.HorizontalExpand = true;
        count.Align = Label.AlignMode.Right;

        return new BoxContainer
        {
            Orientation = LayoutOrientation.Vertical,
            Margin = new Thickness(0, 6, 0, 0),
            Children =
            {
                new BoxContainer
                {
                    Orientation = LayoutOrientation.Horizontal,
                    Children = { name, count },
                },
                new HLine { Color = StyleNano.TerminalGreenDim.WithAlpha(0.22f), Thickness = 1 },
            },
        };
    }

    private Control EntryCard(ChangelogEntry entry)
    {
        var author = TermLabel(entry.Author, 11);
        author.HorizontalExpand = true;
        author.ClipText = true;

        var header = new BoxContainer
        {
            Orientation = LayoutOrientation.Horizontal,
            SeparationOverride = 6,
            Children = { author },
        };

        header.AddChild(TermLabel(entry.Time.ToLocalTime().ToString("HH:mm"), 10, MutedColor));

        var lines = new BoxContainer
        {
            Orientation = LayoutOrientation.Vertical,
            SeparationOverride = 2,
            Margin = new Thickness(0, 3, 0, 0),
        };

        foreach (var change in entry.Changes)
        {
            var msg = new FormattedMessage();
            msg.PushColor(TextColor);
            msg.AddText(change.Message);
            msg.Pop();

            var text = new RichTextLabel { HorizontalExpand = true, VerticalAlignment = VAlignment.Top };
            text.SetMessage(msg);

            lines.AddChild(new BoxContainer
            {
                Orientation = LayoutOrientation.Horizontal,
                SeparationOverride = 4,
                Children = { TypeIcon(change.Type), TypeTag(change.Type), text },
            });
        }

        return new PanelContainer
        {
            HorizontalExpand = true,
            PanelOverride = new StyleBoxFlat
            {
                BackgroundColor = CardColor,
                BorderColor = StyleNano.TerminalGreenDim.WithAlpha(0.55f),
                BorderThickness = new Thickness(2, 0, 0, 0),
            },
            Children =
            {
                new BoxContainer
                {
                    Orientation = LayoutOrientation.Vertical,
                    Margin = new Thickness(8, 5, 6, 6),
                    Children = { header, lines },
                },
            },
        };
    }

    private TextureRect TypeIcon(ChangelogLineType type)
    {
        var file = type switch
        {
            ChangelogLineType.Add => "plus.svg.192dpi.png",
            ChangelogLineType.Remove => "minus.svg.192dpi.png",
            ChangelogLineType.Fix => "bug.svg.192dpi.png",
            _ => "wrench.svg.192dpi.png",
        };

        return new TextureRect
        {
            Texture = _resCache.GetTexture(new ResPath($"/Textures/Interface/Changelog/{file}")),
            TextureScale = new Vector2(0.45f, 0.45f),
            VerticalAlignment = VAlignment.Top,
            Margin = new Thickness(0, 3, 0, 0),
            ModulateSelfOverride = TypeColor(type),
        };
    }

    private Label TypeTag(ChangelogLineType type)
    {
        var label = TermLabel(Loc.GetString(TypeLocKey(type)), 9, TypeColor(type));
        label.MinWidth = 38;
        label.VerticalAlignment = VAlignment.Top;
        label.Margin = new Thickness(0, 2, 0, 0);
        return label;
    }

    private static Color TypeColor(ChangelogLineType type)
    {
        return type switch
        {
            ChangelogLineType.Add => Color.FromHex("#7FD38F"),
            ChangelogLineType.Remove => Color.FromHex("#D9776B"),
            ChangelogLineType.Fix => Color.FromHex("#E3C26A"),
            _ => Color.FromHex("#7FA6D9"),
        };
    }

    private static string TypeLocKey(ChangelogLineType type)
    {
        return type switch
        {
            ChangelogLineType.Add => "blackm-changelog-type-add",
            ChangelogLineType.Remove => "blackm-changelog-type-remove",
            ChangelogLineType.Fix => "blackm-changelog-type-fix",
            _ => "blackm-changelog-type-tweak",
        };
    }

    private void ShowMessage(string text)
    {
        var label = TermLabel(text, 11, MutedColor);
        label.HorizontalAlignment = HAlignment.Center;
        label.Margin = new Thickness(0, 16);
        _body.AddChild(label);
    }

    private Label TermLabel(string text, int size, Color? color = null)
    {
        return new Label
        {
            Text = text,
            StyleClasses = { StyleNano.StyleClassLabelTerminalBlackM },
            FontOverride = TerminalFonts.Bold(_resCache, size),
            FontColorOverride = color ?? StyleNano.TerminalGreen,
        };
    }

    private Button TermButton(string text)
    {
        var button = new Button
        {
            Text = text,
            StyleClasses = { StyleNano.StyleClassFlatButtonTerminalBlackM },
            MinHeight = 24,
        };
        TerminalSounds.Attach(button);
        return button;
    }
}
