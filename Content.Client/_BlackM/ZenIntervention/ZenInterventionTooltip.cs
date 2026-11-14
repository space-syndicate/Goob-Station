using System;
using System.Linq;
using System.Text;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Maths;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Client._BlackM.ZenIntervention;

public sealed class ZenInterventionTooltip : PanelContainer
{
    private const string Dim = "#8d90ad";
    private const string Light = "#e6e8f2";

    [Dependency] private readonly IGameTiming _timing = default!;

    private readonly ZenInterventionBarControl _bar;
    private readonly Label _title;
    private readonly RichTextLabel _level;
    private readonly RichTextLabel _stages;
    private readonly RichTextLabel _footer;

    private float _refreshAccumulator;
    private string _lastLevel = string.Empty;
    private string _lastStages = string.Empty;
    private string _lastFooter = string.Empty;

    public ZenInterventionTooltip(ZenInterventionBarControl bar)
    {
        IoCManager.InjectDependencies(this);
        _bar = bar;

        PanelOverride = new StyleBoxFlat
        {
            BackgroundColor = Color.FromHex("#2b303a").WithAlpha(0.97f),
            BorderColor = Color.FromHex("#7b7e9e"),
            BorderThickness = new Thickness(1),
            ContentMarginLeftOverride = 10,
            ContentMarginRightOverride = 10,
            ContentMarginTopOverride = 8,
            ContentMarginBottomOverride = 8,
        };

        _title = new Label { Text = Loc.GetString("zen-intervention-ui-tooltip-title") };
        _title.StyleClasses.Add("LabelHeading");

        var desc = new RichTextLabel();
        desc.SetMessage(FormattedMessage.FromMarkupPermissive(
            $"[color={Dim}]{Loc.GetString("zen-intervention-ui-tooltip-desc")}[/color]"));

        _level = new RichTextLabel();
        _stages = new RichTextLabel();
        _footer = new RichTextLabel();

        var root = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            SeparationOverride = 6,
            MinWidth = 320,
            MaxWidth = 320,
        };

        root.AddChild(_title);
        root.AddChild(desc);
        root.AddChild(Separator());
        root.AddChild(_level);
        root.AddChild(_stages);
        root.AddChild(Separator());
        root.AddChild(_footer);

        AddChild(root);
        Refresh();
    }

    private static Control Separator()
    {
        return new PanelContainer
        {
            MinHeight = 1,
            PanelOverride = new StyleBoxFlat { BackgroundColor = Color.FromHex("#4a5163") },
        };
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        _refreshAccumulator += args.DeltaSeconds;
        if (_refreshAccumulator < 0.2f)
            return;

        _refreshAccumulator = 0f;
        Refresh();
    }

    private void Refresh()
    {
        var s = _bar.State;
        var color = ZenInterventionPalette.ForStage(s.Stage);
        _title.FontColorOverride = color;

        Set(_level, BuildLevel(s), ref _lastLevel);
        Set(_stages, BuildStages(s), ref _lastStages);
        Set(_footer, BuildFooter(s), ref _lastFooter);
    }

    private static void Set(RichTextLabel label, string markup, ref string cache)
    {
        if (markup == cache)
            return;

        cache = markup;
        label.SetMessage(FormattedMessage.FromMarkupPermissive(markup));
    }

    private static string BuildLevel(ZenBarState s)
    {
        var text = Loc.GetString("zen-intervention-ui-tooltip-level",
            ("level", ((int) MathF.Floor(s.Fraction * 100f)).ToString()),
            ("gain", s.GainPerMinute.ToString("0.##")));

        return $"[color={Light}]{text}[/color]";
    }

    private static string BuildStages(ZenBarState s)
    {
        var sb = new StringBuilder();
        var threshold = ((int) MathF.Round(s.BreachFraction * 100f)).ToString();

        Append(sb, s.Stage == ZenStage.Calm, ZenStage.Calm,
            Loc.GetString("zen-intervention-ui-stage-calm-title", ("threshold", threshold)),
            Loc.GetString("zen-intervention-ui-stage-calm-desc",
                ("thresholds", string.Join("%, ", s.IntermediateBreachThresholds.Select(t => t.ToString("0.##")))),
                ("count", s.IntermediateBreachSpawnAmount)));

        Append(sb, s.Stage == ZenStage.Breach, ZenStage.Breach,
            Loc.GetString("zen-intervention-ui-stage-breach-title", ("threshold", threshold)),
            Loc.GetString("zen-intervention-ui-stage-breach-desc", ("count", s.BreachSpawnAmount.ToString())));

        Append(sb, s.Stage == ZenStage.Cascade, ZenStage.Cascade,
            Loc.GetString("zen-intervention-ui-stage-cascade-title"),
            Loc.GetString("zen-intervention-ui-stage-cascade-desc",
                ("minutes", ((int) MathF.Max(1f, MathF.Round((float) s.WaveInterval.TotalMinutes))).ToString()),
                ("count", s.WaveSpawnAmount.ToString())));

        return sb.ToString().TrimEnd('\n');
    }

    private static void Append(StringBuilder sb, bool current, ZenStage stage, string title, string desc)
    {
        var head = current ? ZenInterventionPalette.HexForStage(stage) : Dim;
        var body = current ? Light : Dim;
        var mark = current ? "» " : "   ";

        sb.Append($"[color={head}][bold]{mark}{title}[/bold][/color]\n");
        sb.Append($"[color={body}]{desc}[/color]\n\n");
    }

    private string BuildFooter(ZenBarState s)
    {
        var color = ZenInterventionPalette.HexForStage(s.Stage);
        string text;

        if (!s.Enabled)
        {
            text = Loc.GetString("zen-intervention-ui-paused");
        }
        else if (s.Stage == ZenStage.Cascade)
        {
            if (!s.WaveActive)
            {
                text = Loc.GetString("zen-intervention-ui-wave-pending");
            }
            else
            {
                var left = s.NextWaveTime - _timing.CurTime;
                if (left < TimeSpan.Zero)
                    left = TimeSpan.Zero;

                text = Loc.GetString("zen-intervention-ui-next-wave",
                    ("time", $"{(int) left.TotalMinutes:00}:{left.Seconds:00}"));
            }
        }
        else if (s.GainPerMinute <= 0f)
        {
            text = Loc.GetString("zen-intervention-ui-paused");
        }
        else
        {
            var target = s.Stage == ZenStage.Calm ? s.BreachThreshold : s.MaxLevel;
            if (s.Stage == ZenStage.Calm)
            {
                foreach (var threshold in s.IntermediateBreachThresholds)
                {
                    if (threshold > s.Level && threshold < target)
                        target = threshold;
                }
            }
            var minutes = (int) MathF.Max(1f, MathF.Ceiling((target - s.Level) / s.GainPerMinute));
            var key = s.Stage == ZenStage.Calm
                ? "zen-intervention-ui-next-breach"
                : "zen-intervention-ui-next-cascade";

            text = Loc.GetString(key, ("minutes", minutes.ToString()));
        }

        return $"[color={color}][bold]{text}[/bold][/color]";
    }
}
