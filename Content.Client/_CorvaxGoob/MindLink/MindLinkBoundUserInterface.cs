using Content.Client.UserInterface.Controls;
using Content.Shared._CorvaxGoob.MindLink;
using JetBrains.Annotations;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Content.Client._CorvaxGoob.MindLink;

[UsedImplicitly]
public sealed class MindLinkBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    private const float MaxTargetListHeight = 500f;

    private FancyWindow? _window;

    protected override void Open()
    {
        base.Open();
        _window = new FancyWindow { MinWidth = 320 };
        _window.OnClose += Close;
        _window.OpenCentered();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);
        if (_window == null || state is not MindLinkBuiState mindLinkState)
            return;

        _window.ContentsContainer.Children.Clear();
        if (mindLinkState.RecipientName is null)
            ShowTargetSelection(mindLinkState);
        else
            ShowMessageInput(mindLinkState);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
            _window?.Close();
    }

    private void ShowTargetSelection(MindLinkBuiState state)
    {
        _window!.Title = Loc.GetString(state.IsReply
            ? "mind-link-reply-target-window-title"
            : "mind-link-target-window-title");
        var list = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            HorizontalExpand = true,
        };
        if (state.Targets.Count == 0)
            list.AddChild(new Label { Text = Loc.GetString("mind-link-no-targets") });

        if (state.CanSendToAll)
        {
            var all = new Button { Text = Loc.GetString("mind-link-all-targets") };
            all.OnPressed += _ => SendMessage(new SelectAllMindLinkTargetsMessage());
            list.AddChild(all);
        }

        foreach (var target in state.Targets)
        {
            var button = new Button { Text = target.Name, HorizontalExpand = true };
            button.OnPressed += _ => SendMessage(new SelectMindLinkTargetMessage(target.Entity));
            if (!target.Connected)
            {
                list.AddChild(button);
                continue;
            }

            var row = new BoxContainer
            {
                Orientation = BoxContainer.LayoutOrientation.Horizontal,
                HorizontalExpand = true,
            };
            row.AddChild(button);
            var disconnect = new Button { Text = "×", ToolTip = Loc.GetString("mind-link-disconnect") };
            disconnect.OnPressed += _ => SendMessage(new DisconnectMindLinkTargetMessage(target.Entity));
            row.AddChild(disconnect);
            list.AddChild(row);
        }

        var scroll = new ScrollContainer
        {
            MaxHeight = MaxTargetListHeight,
            HorizontalExpand = true,
            HScrollEnabled = false,
            ReserveScrollbarSpace = true,
            ReturnMeasure = true,
        };
        scroll.AddChild(list);
        _window.ContentsContainer.AddChild(scroll);
    }

    private void ShowMessageInput(MindLinkBuiState state)
    {
        var recipientName = state.RecipientName ?? string.Empty;
        _window!.Title = Loc.GetString(state.IsReply
            ? "mind-link-reply-window-title"
            : "mind-link-message-window-title", ("target", recipientName));

        var box = new BoxContainer { Orientation = BoxContainer.LayoutOrientation.Vertical };
        var input = new LineEdit { PlaceHolder = Loc.GetString("mind-link-message-placeholder") };
        var send = new Button { Text = Loc.GetString("mind-link-send") };
        void Submit()
        {
            if (!string.IsNullOrWhiteSpace(input.Text))
            {
                SendMessage(new SendMindLinkMessage(input.Text));
                input.Text = string.Empty;
            }
        }

        input.OnTextEntered += _ => Submit();
        send.OnPressed += _ => Submit();
        box.AddChild(input);
        box.AddChild(send);
        _window.ContentsContainer.AddChild(box);
        input.GrabKeyboardFocus();
    }
}
