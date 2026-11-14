using System;
using Content.Client.Gameplay;
using Content.Shared._BlackM.Elevator;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controllers;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.GameObjects;
using Robust.Shared.Timing;

namespace Content.Client._BlackM.Elevator;

public sealed class EvacuationObserverUIController : UIController,
    IOnStateEntered<GameplayState>, IOnStateExited<GameplayState>
{
    [Dependency] private readonly IEntityManager _entities = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IEntityNetworkManager _net = default!;

    private PanelContainer? _panel;
    private Button? _toggle;
    private bool _observing;

    public void OnStateEntered(GameplayState state)
    {
        DestroyPanel();
        _panel = new PanelContainer
        {
            Visible = false,
            PanelOverride = new StyleBoxFlat
            {
                BackgroundColor = Color.FromHex("#242321").WithAlpha(0.97f),
                BorderColor = Color.FromHex("#a18b5d"),
                BorderThickness = new Thickness(1),
                ContentMarginLeftOverride = 12,
                ContentMarginRightOverride = 12,
                ContentMarginTopOverride = 8,
                ContentMarginBottomOverride = 8,
            },
        };

        var content = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            SeparationOverride = 6,
        };
        var title = new Label { Text = Loc.GetString("blackm-evac-observer-title") };
        title.StyleClasses.Add("LabelHeading");
        var message = new RichTextLabel();
        message.SetMessage(Loc.GetString("blackm-evac-observer-message"));
        _toggle = new Button
        {
            Text = Loc.GetString("blackm-evac-observer-watch"),
            HorizontalAlignment = Control.HAlignment.Right,
            MinWidth = 160,
        };
        _toggle.OnPressed += _ => _net.SendSystemNetworkMessage(new EvacuationObserverRequestEvent(_observing));
        content.AddChild(title);
        content.AddChild(message);
        content.AddChild(_toggle);
        _panel.AddChild(content);
        LayoutContainer.SetAnchorLeft(_panel, 0f);
        LayoutContainer.SetAnchorRight(_panel, 0f);
        LayoutContainer.SetAnchorTop(_panel, 0f);
        LayoutContainer.SetAnchorBottom(_panel, 0f);
    }

    public void OnStateExited(GameplayState state)
    {
        DestroyPanel();
    }

    private void DestroyPanel()
    {
        _panel?.Orphan();
        _panel?.Dispose();
        _panel = null;
        _toggle = null;
    }

    public override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);
        if (_panel == null || _toggle == null)
            return;

        if (_player.LocalEntity is not { } uid ||
            !_entities.TryGetComponent<EvacuationObserverComponent>(uid, out var observer) ||
            UIManager.ActiveScreen is not { } screen)
        {
            _panel.Visible = false;
            return;
        }

        if (_panel.Parent != screen)
        {
            _panel.Orphan();
            screen.AddChild(_panel);
        }

        _observing = observer.IsObserving;
        _toggle.Text = Loc.GetString(_observing ? "blackm-evac-observer-return" : "blackm-evac-observer-watch");
        var width = MathF.Min(560f, MathF.Max(0f, screen.Width - 24f));
        var height = width < 420f ? 180f : 140f;
        var left = (screen.Width - width) / 2f;
        var bottom = MathF.Max(height, screen.Height - 120f);
        LayoutContainer.SetMarginLeft(_panel, left);
        LayoutContainer.SetMarginRight(_panel, left + width);
        LayoutContainer.SetMarginTop(_panel, bottom - height);
        LayoutContainer.SetMarginBottom(_panel, bottom);
        _panel.Visible = true;
    }
}
