using System;
using System.Collections.Generic;
using System.Numerics;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Maths;
using Robust.Shared.Timing;

namespace Content.Client._BlackM.ZenIntervention;

public enum ZenStage
{
    Calm,
    Breach,
    Cascade,
}

public readonly record struct ZenBarState(
    float Level,
    float MaxLevel,
    float GainPerMinute,
    float BreachThreshold,
    bool Enabled,
    bool BreachTriggered,
    bool WaveActive,
    TimeSpan NextWaveTime,
    TimeSpan WaveInterval,
    int BreachSpawnAmount,
    int WaveSpawnAmount,
    IReadOnlyList<float> IntermediateBreachThresholds,
    int IntermediateBreachSpawnAmount)
{
    public float Fraction => MaxLevel > 0f ? Math.Clamp(Level / MaxLevel, 0f, 1f) : 0f;
    public float BreachFraction => MaxLevel > 0f ? Math.Clamp(BreachThreshold / MaxLevel, 0f, 1f) : 0.6f;

    public ZenStage Stage =>
        Level >= MaxLevel - 0.001f ? ZenStage.Cascade
        : Level >= BreachThreshold ? ZenStage.Breach
        : ZenStage.Calm;
}

public static class ZenInterventionPalette
{
    public const string CalmHex = "#5fd38d";
    public const string BreachHex = "#e8b83c";
    public const string CascadeHex = "#e5484d";

    public static readonly Color Calm = Color.FromHex(CalmHex);
    public static readonly Color Breach = Color.FromHex(BreachHex);
    public static readonly Color Cascade = Color.FromHex(CascadeHex);

    public static Color ForStage(ZenStage stage) => stage switch
    {
        ZenStage.Cascade => Cascade,
        ZenStage.Breach => Breach,
        _ => Calm,
    };

    public static string HexForStage(ZenStage stage) => stage switch
    {
        ZenStage.Cascade => CascadeHex,
        ZenStage.Breach => BreachHex,
        _ => CalmHex,
    };
}

public sealed class ZenInterventionBarControl : PanelContainer
{
    public const float DesignWidth = 36f;
    public const float DesignHeight = 62f;

    internal const float IconSize = 32f;
    internal const float IconLeft = (DesignWidth - IconSize) / 2f;
    internal const float IconTop = 6f;
    private const int CrystalRows = 31;

    internal const float DigitsTop = 46f;
    private const int DigitCellW = 8;
    internal const int DigitCellH = 10;

    private const string CrystalPath = "/Textures/_BlackM/Interface/ZenIntervention/zen_crystal.png";
    private const string DigitsPath = "/Textures/_BlackM/Interface/ZenIntervention/zen_digits.png";

    private static readonly Color TileColor = Color.FromHex("#3c4350");
    private static readonly Color TileHover = Color.FromHex("#4a5266");
    private static readonly Color TileBorder = Color.FromHex("#242830");
    private static readonly Color Lavender = Color.FromHex("#7b7e9e");
    private static readonly Color CrystalEmpty = Color.FromHex("#666b8a");

    [Dependency] private readonly IResourceCache _resourceCache = default!;

    private readonly StyleBoxFlat _panelStyle;
    private Texture? _crystal;
    private Texture? _digits;

    private bool _hasState;
    private bool _hovered;
    private float _time;
    private float _displayFraction;
    private Color _color = ZenInterventionPalette.Calm;

    public ZenBarState State { get; private set; }

    public ZenInterventionBarControl()
    {
        IoCManager.InjectDependencies(this);

        MinSize = new Vector2(DesignWidth, DesignHeight);

        MouseFilter = MouseFilterMode.Stop;
        TooltipDelay = 0.1f;
        TooltipSupplier = _ => new ZenInterventionTooltip(this);

        _panelStyle = new StyleBoxFlat
        {
            BackgroundColor = TileColor,
            BorderColor = TileBorder,
            BorderThickness = new Thickness(1),
        };
        PanelOverride = _panelStyle;

        if (_resourceCache.TryGetResource<TextureResource>(CrystalPath, out var crystal))
            _crystal = crystal.Texture;

        if (_resourceCache.TryGetResource<TextureResource>(DigitsPath, out var digits))
            _digits = digits.Texture;
    }

    public void SetState(ZenBarState state)
    {
        State = state;

        if (!_hasState)
        {
            _displayFraction = state.Fraction;
            _color = ZenInterventionPalette.ForStage(state.Stage);
            _hasState = true;
        }
    }

    protected override void MouseEntered()
    {
        base.MouseEntered();
        _hovered = true;
    }

    protected override void MouseExited()
    {
        base.MouseExited();
        _hovered = false;
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        var dt = args.DeltaSeconds;
        _time += dt;

        var target = State.Fraction;
        _displayFraction += (target - _displayFraction) * MathF.Min(1f, dt * 6f);
        if (MathF.Abs(target - _displayFraction) < 0.0005f)
            _displayFraction = target;

        _color = Color.InterpolateBetween(_color, ZenInterventionPalette.ForStage(State.Stage), MathF.Min(1f, dt * 4f));

        var stage = State.Stage;
        var blink = stage == ZenStage.Cascade && ((int) (_time * 4f)) % 2 == 0;

        var tile = _hovered ? TileHover : TileColor;
        if (stage == ZenStage.Cascade)
            tile = Color.InterpolateBetween(tile, ZenInterventionPalette.Cascade, blink ? 0.18f : 0.06f);

        _panelStyle.BackgroundColor = tile;
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        base.Draw(handle);

        var px = PixelSize;
        if (px.X <= 0 || px.Y <= 0)
            return;

        var s = MathF.Min(px.X / DesignWidth, px.Y / DesignHeight);
        UIBox2 B(float l, float t, float r, float b) =>
            new(MathF.Round(l * s), MathF.Round(t * s), MathF.Round(r * s), MathF.Round(b * s));

        var stage = State.Stage;
        var blink = stage == ZenStage.Cascade && ((int) (_time * 4f)) % 2 == 0;

        var jitter = 0f;
        if (stage == ZenStage.Cascade)
            jitter = ((int) (_time * 14f) % 4) switch { 1 => 1f, 3 => -1f, _ => 0f };

        if (_crystal != null)
        {
            var box = B(IconLeft + jitter, IconTop, IconLeft + jitter + IconSize, IconTop + IconSize);
            handle.DrawTextureRect(_crystal, box, CrystalEmpty);

            var rows = (int) MathF.Round(_displayFraction * CrystalRows);
            if (rows > 0)
            {
                var ps = box.Width / _crystal.Width;
                var fillColor = blink ? Color.InterpolateBetween(_color, Color.White, 0.35f) : _color;

                var dst = new UIBox2(box.Left, MathF.Round(box.Bottom - rows * ps), box.Right, box.Bottom);
                var src = new UIBox2(0, _crystal.Height - rows, _crystal.Width, _crystal.Height);
                handle.DrawTextureRectRegion(_crystal, dst, src, fillColor);

                var topDst = new UIBox2(dst.Left, dst.Top, dst.Right, MathF.Min(dst.Bottom, dst.Top + ps));
                var topSrc = new UIBox2(0, _crystal.Height - rows, _crystal.Width, _crystal.Height - rows + 1);
                handle.DrawTextureRectRegion(_crystal, topDst, topSrc, Color.InterpolateBetween(fillColor, Color.White, 0.5f));
            }
        }

        DrawPercent(handle, B, (int) MathF.Floor(State.Fraction * 100f),
            stage == ZenStage.Calm ? Lavender : ZenInterventionPalette.ForStage(stage));
    }

    private void DrawPercent(DrawingHandleScreen handle, Func<float, float, float, float, UIBox2> box, int value, Color color)
    {
        if (_digits == null)
            return;

        var text = value + "%";
        var x = (DesignWidth - text.Length * DigitCellW) / 2f;

        foreach (var ch in text)
        {
            var idx = ch == '%' ? 10 : ch - '0';
            if (idx >= 0 && idx <= 10)
            {
                var src = new UIBox2(idx * DigitCellW, 0, (idx + 1) * DigitCellW, DigitCellH);
                handle.DrawTextureRectRegion(_digits, box(x, DigitsTop, x + DigitCellW, DigitsTop + DigitCellH), src, color);
            }

            x += DigitCellW;
        }
    }
}
