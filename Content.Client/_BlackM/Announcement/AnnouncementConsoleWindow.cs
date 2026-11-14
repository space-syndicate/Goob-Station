using System.Linq;
using System.Numerics;
using Content.Shared._BlackM.Announcement;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Client._BlackM.Announcement;

public sealed class AnnouncementConsoleWindow : DefaultWindow
{
    [Dependency] private readonly IGameTiming _timing = default!;

    public event Action<string, string>? OnSendPressed;

    private static readonly Color AccentColor = Color.FromHex("#5fa8d3");
    private static readonly Color PanelBg = Color.FromHex("#1b1f24");
    private static readonly Color PanelBgLight = Color.FromHex("#242a31");
    private static readonly Color MutedColor = Color.FromHex("#8a94a3");
    private static readonly Color WarnColor = Color.FromHex("#d36a5f");
    private static readonly Color ButtonIdleColor = Color.FromHex("#4a525c");

    private readonly StyleBoxFlat _headerStyle;
    private readonly Label _senderLabel;
    private readonly GridContainer _channelGrid;
    private readonly Label _channelHint;
    private readonly TextEdit _input;
    private readonly Label _counterLabel;
    private readonly Label _statusLabel;
    private readonly Button _sendButton;

    private readonly Dictionary<string, Button> _buttons = new();
    private List<AnnouncementChannelEntry> _channels = new();
    private string? _selected;
    private TimeSpan _next;
    private int _maxLength = 256;
    private bool _hasAccess;
    private bool _awaitingResult;

    public AnnouncementConsoleWindow()
    {
        IoCManager.InjectDependencies(this);

        MinSize = new Vector2(440, 400);
        SetSize = new Vector2(480, 450);

        var root = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            HorizontalExpand = true,
            VerticalExpand = true,
            SeparationOverride = 6,
        };

        _headerStyle = new StyleBoxFlat
        {
            BackgroundColor = PanelBgLight,
            BorderColor = AccentColor,
            BorderThickness = new Thickness(0, 0, 0, 2),
            ContentMarginLeftOverride = 10,
            ContentMarginRightOverride = 10,
            ContentMarginTopOverride = 6,
            ContentMarginBottomOverride = 6,
        };

        var header = new PanelContainer { PanelOverride = _headerStyle };
        var headerBox = new BoxContainer { Orientation = BoxContainer.LayoutOrientation.Vertical };
        headerBox.AddChild(new Label
        {
            Text = Loc.GetString("blackm-announce-window-sender"),
            FontColorOverride = MutedColor,
        });
        _senderLabel = new Label { Text = "-", ClipText = true };
        headerBox.AddChild(_senderLabel);
        header.AddChild(headerBox);
        root.AddChild(header);

        root.AddChild(new Label
        {
            Text = Loc.GetString("blackm-announce-window-channel"),
            FontColorOverride = MutedColor,
        });

        _channelGrid = new GridContainer
        {
            Columns = 3,
            HorizontalExpand = true,
            HSeparationOverride = 4,
            VSeparationOverride = 4,
        };
        root.AddChild(_channelGrid);

        _channelHint = new Label { FontColorOverride = MutedColor };
        root.AddChild(_channelHint);

        root.AddChild(new Label
        {
            Text = Loc.GetString("blackm-announce-window-text"),
            FontColorOverride = MutedColor,
        });

        var inputPanel = new PanelContainer
        {
            VerticalExpand = true,
            HorizontalExpand = true,
            PanelOverride = new StyleBoxFlat
            {
                BackgroundColor = PanelBg,
                ContentMarginLeftOverride = 4,
                ContentMarginRightOverride = 4,
                ContentMarginTopOverride = 4,
                ContentMarginBottomOverride = 4,
            },
        };
        _input = new TextEdit
        {
            HorizontalExpand = true,
            VerticalExpand = true,
            MinHeight = 110,
        };
        _input.Placeholder = new Rope.Leaf(Loc.GetString("blackm-announce-window-placeholder"));
        inputPanel.AddChild(_input);
        root.AddChild(inputPanel);

        var bottom = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            HorizontalExpand = true,
            SeparationOverride = 8,
        };
        _counterLabel = new Label { FontColorOverride = MutedColor, VerticalAlignment = VAlignment.Center };
        _statusLabel = new Label
        {
            HorizontalExpand = true,
            Align = Label.AlignMode.Right,
            FontColorOverride = WarnColor,
            VerticalAlignment = VAlignment.Center,
        };
        _sendButton = new Button
        {
            Text = Loc.GetString("blackm-announce-window-send"),
            MinWidth = 110,
            Disabled = true,
        };
        _sendButton.OnPressed += _ => Send();

        bottom.AddChild(_counterLabel);
        bottom.AddChild(_statusLabel);
        bottom.AddChild(_sendButton);
        root.AddChild(bottom);

        Contents.AddChild(root);
    }

    public void UpdateState(AnnouncementConsoleBuiState state)
    {
        if (_awaitingResult && state.NextAnnouncement > _next)
        {
            _input.TextRope = Rope.Leaf.Empty;
            _awaitingResult = false;
        }

        _next = state.NextAnnouncement;
        _maxLength = state.MaxLength;
        _hasAccess = state.HasAccess;

        _senderLabel.Text = string.IsNullOrEmpty(state.SenderJob)
            ? state.SenderName
            : Loc.GetString("blackm-announce-sender", ("name", state.SenderName), ("job", state.SenderJob));

        RebuildChannels(state.Channels);
        UpdateControls();
    }

    private void RebuildChannels(List<AnnouncementChannelEntry> channels)
    {
        _channels = channels;
        _channelGrid.RemoveAllChildren();
        _buttons.Clear();

        if (_selected == null || !channels.Any(c => c.Id == _selected && c.Allowed))
            _selected = channels.FirstOrDefault(c => c.Allowed)?.Id;

        foreach (var channel in channels)
        {
            var id = channel.Id;
            var button = new Button
            {
                Text = channel.Name,
                ToggleMode = true,
                HorizontalExpand = true,
                Disabled = !channel.Allowed,
                ToolTip = channel.Allowed ? null : Loc.GetString("blackm-announce-window-no-channel-access"),
            };
            button.OnPressed += _ => Select(id);

            _buttons[id] = button;
            _channelGrid.AddChild(button);
        }

        SyncButtons();
    }

    private void Select(string id)
    {
        _selected = id;
        SyncButtons();
        UpdateControls();
    }

    private void SyncButtons()
    {
        foreach (var (id, button) in _buttons)
        {
            var pressed = id == _selected;
            button.Pressed = pressed;

            if (button.Disabled)
            {
                button.ModulateSelfOverride = null;
                button.Label.FontColorOverride = null;
                continue;
            }

            var accent = _channels.FirstOrDefault(c => c.Id == id)?.Accent ?? AccentColor;
            button.ModulateSelfOverride = pressed ? accent : ButtonIdleColor;
            button.Label.FontColorOverride = pressed ? PanelBg : accent;
        }

        var selected = _channels.FirstOrDefault(c => c.Id == _selected);
        _headerStyle.BorderColor = selected?.Accent ?? AccentColor;
        _channelHint.Text = selected == null
            ? Loc.GetString("blackm-announce-window-no-channels")
            : Loc.GetString("blackm-announce-window-channel-selected", ("channel", selected.Name));
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);
        UpdateControls();
    }

    private void UpdateControls()
    {
        var length = Rope.Collapse(_input.TextRope).Trim().Length;

        var counter = $"{length}/{_maxLength}";
        if (_counterLabel.Text != counter)
            _counterLabel.Text = counter;
        _counterLabel.FontColorOverride = length > _maxLength ? WarnColor : MutedColor;

        var status = string.Empty;
        var canSend = true;
        var cooldown = _next - _timing.CurTime;

        if (!_hasAccess)
        {
            status = Loc.GetString("blackm-announce-window-no-access");
            canSend = false;
        }
        else if (cooldown > TimeSpan.Zero)
        {
            status = Loc.GetString("blackm-announce-window-cooldown", ("seconds", (int) Math.Ceiling(cooldown.TotalSeconds)));
            canSend = false;
        }
        else if (_selected == null)
        {
            canSend = false;
        }

        if (length == 0 || length > _maxLength)
            canSend = false;

        if (_statusLabel.Text != status)
            _statusLabel.Text = status;

        _sendButton.Disabled = !canSend;
    }

    private void Send()
    {
        if (_selected == null)
            return;

        var text = Rope.Collapse(_input.TextRope).Trim();
        if (text.Length == 0 || text.Length > _maxLength)
            return;

        _awaitingResult = true;
        OnSendPressed?.Invoke(_selected, text);
    }
}
