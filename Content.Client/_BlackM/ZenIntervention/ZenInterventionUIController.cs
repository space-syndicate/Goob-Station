using Content.Client.Gameplay;
using Content.Client.UserInterface.Systems.MenuBar.Widgets;
using Content.Shared._BlackM.ZenCatalyst;
using Content.Shared._BlackM.ZenIntervention;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controllers;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Timing;

namespace Content.Client._BlackM.ZenIntervention;

public sealed class ZenInterventionUIController : UIController, IOnStateEntered<GameplayState>, IOnStateExited<GameplayState>
{
    private const float GapFromToolbar = 4f;

    [Dependency] private readonly IEntityManager _entityManager = default!;

    private ZenInterventionBarControl? _bar;
    private ZenCatalystIndicatorControl? _catalyst;
    private float _lastX = float.NaN;
    private float _lastY = float.NaN;
    private float _lastScale = float.NaN;
    private bool _lastHasIntervention;
    private bool _lastHasCatalyst;

    private GameTopMenuBar? TopMenuBar => UIManager.GetActiveUIWidgetOrNull<GameTopMenuBar>();

    public void OnStateEntered(GameplayState state)
    {
        DestroyBar();

        _lastX = float.NaN;
        _lastY = float.NaN;
        _lastScale = float.NaN;

        _bar = new ZenInterventionBarControl
        {
            Visible = false,
        };
        _catalyst = new ZenCatalystIndicatorControl
        {
            Visible = false,
        };

        LayoutContainer.SetAnchorLeft(_bar, 0f);
        LayoutContainer.SetAnchorRight(_bar, 0f);
        LayoutContainer.SetAnchorTop(_bar, 0f);
        LayoutContainer.SetAnchorBottom(_bar, 0f);
        LayoutContainer.SetAnchorLeft(_catalyst, 0f);
        LayoutContainer.SetAnchorRight(_catalyst, 0f);
        LayoutContainer.SetAnchorTop(_catalyst, 0f);
        LayoutContainer.SetAnchorBottom(_catalyst, 0f);

        AttachToScreen();
    }

    public void OnStateExited(GameplayState state)
    {
        DestroyBar();
    }

    private void DestroyBar()
    {
        if (_bar?.Parent != null)
            _bar.Orphan();
        _bar?.Dispose();
        _bar = null;
        if (_catalyst?.Parent != null)
            _catalyst.Orphan();
        _catalyst?.Dispose();
        _catalyst = null;
    }

    private UIScreen? AttachToScreen()
    {
        var screen = UIManager.ActiveScreen;
        if (screen == null || _bar == null || _catalyst == null)
            return null;

        if (_bar.Parent != screen)
        {
            if (_bar.Parent != null)
                _bar.Orphan();

            screen.AddChild(_bar);
        }
        if (_catalyst.Parent != screen)
        {
            if (_catalyst.Parent != null)
                _catalyst.Orphan();
            screen.AddChild(_catalyst);
        }

        return screen;
    }

    public override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        if (_bar == null || _catalyst == null)
            return;

        var query = _entityManager.EntityQueryEnumerator<ZenInterventionComponent>();
        var hasIntervention = query.MoveNext(out _, out var comp);
        var catalystQuery = _entityManager.EntityQueryEnumerator<ZenCatalystStatusComponent>();
        var hasCatalyst = catalystQuery.MoveNext(out _, out var catalystStatus);
        if (!hasIntervention && !hasCatalyst)
        {
            _bar.Visible = _catalyst.Visible = false;
            return;
        }

        var screen = AttachToScreen();
        var topBar = TopMenuBar;
        if (screen == null || topBar == null || !topBar.Visible)
        {
            _bar.Visible = _catalyst.Visible = false;
            return;
        }

        if (hasIntervention && comp != null)
        {
            _bar.SetState(new ZenBarState(
                comp.Level,
                comp.MaxLevel,
                comp.GainPerMinute,
                comp.BreachThreshold,
                comp.Enabled,
                comp.BreachTriggered,
                comp.WaveActive,
                comp.NextWaveTime,
                comp.WaveInterval,
                comp.BreachSpawnAmount,
                comp.WaveSpawnAmount,
                comp.IntermediateBreachThresholds,
                comp.IntermediateBreachSpawnAmount));
        }
        if (hasCatalyst && catalystStatus != null)
            _catalyst.SetState(catalystStatus.Catalysts);

        var pos = topBar.GlobalPosition - screen.GlobalPosition;

        var k = topBar.Height > 0f ? topBar.Height / ZenInterventionBarControl.DesignHeight : 1f;
        var barW = ZenInterventionBarControl.DesignWidth * k;
        var barH = ZenInterventionBarControl.DesignHeight * k;
        var catalystW = ZenCatalystIndicatorControl.DesignWidth * k;
        var gap = GapFromToolbar * k;
        var totalW = (hasIntervention ? barW : 0) + (hasCatalyst ? catalystW : 0) +
            (hasIntervention && hasCatalyst ? gap : 0);

        var x = pos.X + topBar.Width + GapFromToolbar * k;
        var maxX = screen.Width - totalW - 4f;
        if (x > maxX)
            x = maxX;
        if (x < 0f)
            x = 0f;

        var y = pos.Y + (topBar.Height - barH) / 2f;
        if (y < 0f)
            y = 0f;

        if (x != _lastX || y != _lastY || k != _lastScale ||
            hasIntervention != _lastHasIntervention || hasCatalyst != _lastHasCatalyst)
        {
            _lastX = x;
            _lastY = y;
            _lastScale = k;
            _lastHasIntervention = hasIntervention;
            _lastHasCatalyst = hasCatalyst;

            LayoutContainer.SetMarginLeft(_bar, x);
            LayoutContainer.SetMarginRight(_bar, x + barW);
            LayoutContainer.SetMarginTop(_bar, y);
            LayoutContainer.SetMarginBottom(_bar, y + barH);

            var catalystX = x + (hasIntervention ? barW + gap : 0);
            LayoutContainer.SetMarginLeft(_catalyst, catalystX);
            LayoutContainer.SetMarginRight(_catalyst, catalystX + catalystW);
            LayoutContainer.SetMarginTop(_catalyst, y);
            LayoutContainer.SetMarginBottom(_catalyst, y + barH);
        }

        _bar.Visible = hasIntervention;
        _catalyst.Visible = hasCatalyst;
    }
}
