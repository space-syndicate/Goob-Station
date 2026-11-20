using System.Numerics;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.GameObjects;
using Robust.Shared.Input;
using Robust.Shared.Maths;
using Robust.Shared.Timing;

namespace Content.Client._BlackM.WelcomeMessage;

public enum WelcomeSound : byte
{
    Appear,
    Type,
    Disappear,
}

public sealed record WelcomeFonts(Font Caption, Font Sender, Font Body);

public sealed class WelcomeMessageControl : Control
{
    // Палитра NeedsHud.
    private static readonly Color PanelColor = Color.FromHex("#222935").WithAlpha(0.96f);
    private static readonly Color BorderColor = Color.FromHex("#566273");
    private static readonly Color InsetColor = Color.FromHex("#10151d");
    private static readonly Color InsetBorderColor = Color.FromHex("#3a4352");
    private static readonly Color SenderColor = Color.FromHex("#e5e8ed");
    private static readonly Color BodyColor = Color.FromHex("#c6cfdb");

    public const float TopMargin = 12f;

    private const float TextWidth = 320f;

    private const float PortraitWidth = 120f;
    private const float PortraitMinHeight = 92f;

    private const float StripHeight = 4f;
    private const float StripWidth = 0.12f;

    private const float FadeInTime = 0.08f;
    private const float OpenWidthTime = 0.16f;
    private const float OpenHeightTime = 0.22f;
    private const float TypeDelay = 0.06f;

    private const float CloseHeightTime = 0.18f;
    private const float CloseWidthTime = 0.14f;

    private const float CharsPerSecond = 60f;
    private const int CharsPerTypeSound = 5;
    private const float MinTypeSoundGap = 0.09f;

    private const float MinHold = 3f;
    private const float ReadCharsPerSecond = 25f;

    private enum Phase : byte
    {
        Open,
        Type,
        Hold,
        Close,
        Done,
    }

    public event Action<WelcomeSound>? PlaySound;

    public bool Finished => _phase == Phase.Done;

    private readonly BoxContainer _content;
    private readonly FrameOverlay _frame;
    private readonly List<(Label Label, string Text)> _lines = new();
    private readonly string _fullText;
    private readonly float _hold;

    private Phase _phase = Phase.Open;
    private float _phaseTime;
    private float _typed;
    private int _shown;
    private int _soundCounter;
    private float _sinceTypeSound = MinTypeSoundGap;
    private float _holdLeft;
    private bool _hovered;
    private bool _appearPlayed;

    private float _openX;
    private float _openY;
    private float _laidOutX = -1f;
    private float _laidOutY = -1f;

    public WelcomeMessageControl(string sender, string title, string text, Texture? portrait, Color accent,
        float duration, WelcomeFonts fonts, EntityUid? portraitEntity = null)
    {
        MouseFilter = MouseFilterMode.Stop;
        RectClipContent = true;
        Modulate = Color.White.WithAlpha(0f);

        _content = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            SeparationOverride = 0,
            Margin = new Thickness(3, 1, 1, 3),
            Modulate = Color.White.WithAlpha(0f),
        };
        AddChild(_content);

        if (portrait != null || portraitEntity != null)
        {
            _content.AddChild(new PortraitView(portrait, accent, portraitEntity)
            {
                MinSize = new Vector2(PortraitWidth, PortraitMinHeight),
            });

            _content.AddChild(new PanelContainer
            {
                MinWidth = 1,
                PanelOverride = new StyleBoxFlat { BackgroundColor = InsetBorderColor },
            });
        }

        var column = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            SeparationOverride = 2,
            MinWidth = TextWidth,
            VerticalAlignment = VAlignment.Center,
            Margin = new Thickness(10, 8, 11, 9),
        };
        _content.AddChild(column);

        var header = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            SeparationOverride = 6,
        };
        column.AddChild(header);

        header.AddChild(new Label
        {
            Text = title.ToUpperInvariant(),
            FontOverride = fonts.Caption,
            FontColorOverride = accent,
            HorizontalExpand = true,
            ClipText = true,
        });

        column.AddChild(new Label
        {
            Text = sender,
            FontOverride = fonts.Sender,
            FontColorOverride = SenderColor,
            ClipText = true,
        });

        column.AddChild(new PanelContainer
        {
            MinHeight = 1,
            Margin = new Thickness(0, 3, 0, 4),
            PanelOverride = new StyleBoxFlat { BackgroundColor = BorderColor.WithAlpha(0.6f) },
        });

        var body = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            SeparationOverride = 1,
        };
        column.AddChild(body);

        var lineHeight = MeasureText("Аy", fonts.Body).Y;
        var full = string.Empty;
        foreach (var line in Wrap(text, fonts.Body, TextWidth))
        {
            var label = new Label
            {
                Text = string.Empty,
                FontOverride = fonts.Body,
                FontColorOverride = BodyColor,
                MinSize = new Vector2(TextWidth, lineHeight),
            };
            body.AddChild(label);
            _lines.Add((label, line));
            full += line;
        }

        _fullText = full;

        _frame = new FrameOverlay(accent);
        AddChild(_frame);

        _hold = MathF.Max(MathF.Max(duration, MinHold), 2f + _fullText.Length / ReadCharsPerSecond);
        _holdLeft = _hold;
    }

    public void Skip()
    {
        switch (_phase)
        {
            case Phase.Open:
            case Phase.Type:
                _appearPlayed = true;
                _openX = 1f;
                _openY = 1f;
                Reveal(_fullText.Length, false);
                _typed = _fullText.Length;
                SetPhase(Phase.Hold);
                break;
            case Phase.Hold:
                BeginClose();
                break;
        }
    }

    protected override void KeyBindDown(GUIBoundKeyEventArgs args)
    {
        base.KeyBindDown(args);

        if (args.Function != EngineKeyFunctions.UIClick && args.Function != EngineKeyFunctions.UIRightClick)
            return;

        args.Handle();
        Skip();
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
        _phaseTime += dt;
        _sinceTypeSound += dt;

        switch (_phase)
        {
            case Phase.Open:
                if (!_appearPlayed)
                {
                    _appearPlayed = true;
                    PlaySound?.Invoke(WelcomeSound.Appear);
                }

                _openX = EaseOut(_phaseTime / OpenWidthTime);
                _openY = EaseOut((_phaseTime - OpenWidthTime) / OpenHeightTime);

                if (_phaseTime >= OpenWidthTime + OpenHeightTime + TypeDelay)
                    SetPhase(_fullText.Length > 0 ? Phase.Type : Phase.Hold);
                break;

            case Phase.Type:
                _typed += dt * CharsPerSecond;
                Reveal(Math.Min(_fullText.Length, (int) _typed), true);

                if (_shown >= _fullText.Length)
                    SetPhase(Phase.Hold);
                break;

            case Phase.Hold:
                if (!_hovered)
                    _holdLeft -= dt;

                if (_holdLeft <= 0f)
                    BeginClose();
                break;

            case Phase.Close:
                _openY = 1f - EaseInOut(_phaseTime / CloseHeightTime);
                _openX = 1f - EaseInOut((_phaseTime - CloseHeightTime) / CloseWidthTime);

                if (_phaseTime >= CloseHeightTime + CloseWidthTime)
                    SetPhase(Phase.Done);
                break;
        }

        UpdateVisuals();
    }

    protected override Vector2 MeasureOverride(Vector2 availableSize)
    {
        _content.Measure(Vector2Helpers.Infinity);
        _frame.Measure(Vector2Helpers.Infinity);

        var full = _content.DesiredSize;
        var width = Lerp(full.X * StripWidth, full.X, _openX);
        var height = Lerp(MathF.Min(StripHeight, full.Y), full.Y, _openY);

        return new Vector2(MathF.Round(width), MathF.Round(height));
    }

    protected override Vector2 ArrangeOverride(Vector2 finalSize)
    {
        var full = _content.DesiredSize;
        var x = MathF.Round((finalSize.X - full.X) / 2f);
        _content.Arrange(UIBox2.FromDimensions(new Vector2(x, 0f), full));
        _frame.Arrange(UIBox2.FromDimensions(Vector2.Zero, finalSize));

        return finalSize;
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        base.Draw(handle);
        handle.DrawRect(new UIBox2(0, 0, PixelWidth, PixelHeight), PanelColor);
    }

    private void SetPhase(Phase phase)
    {
        _phase = phase;
        _phaseTime = 0f;
    }

    private void BeginClose()
    {
        if (_phase is Phase.Close or Phase.Done)
            return;

        MouseFilter = MouseFilterMode.Ignore;
        _hovered = false;
        PlaySound?.Invoke(WelcomeSound.Disappear);
        SetPhase(Phase.Close);
    }

    private void Reveal(int count, bool sounds)
    {
        count = Math.Clamp(count, 0, _fullText.Length);
        if (count <= _shown)
            return;

        var start = 0;
        foreach (var (label, line) in _lines)
        {
            var end = start + line.Length;
            if (count > start && _shown < end)
            {
                var visible = Math.Clamp(count - start, 0, line.Length);
                if (visible > 0 && visible < line.Length && char.IsHighSurrogate(line[visible - 1]))
                    visible--;

                label.Text = line.Substring(0, visible);
            }

            start = end;
        }

        if (sounds)
        {
            for (var i = _shown; i < count; i++)
            {
                if (!char.IsWhiteSpace(_fullText[i]))
                    _soundCounter++;
            }

            if (_soundCounter >= CharsPerTypeSound && _sinceTypeSound >= MinTypeSoundGap)
            {
                _soundCounter = 0;
                _sinceTypeSound = 0f;
                PlaySound?.Invoke(WelcomeSound.Type);
            }
        }

        _shown = count;
    }

    private void UpdateVisuals()
    {
        var alpha = _phase switch
        {
            Phase.Open => Math.Clamp(_phaseTime / FadeInTime, 0f, 1f),
            Phase.Close => 1f - Math.Clamp((_phaseTime - CloseHeightTime) / CloseWidthTime, 0f, 1f),
            Phase.Done => 0f,
            _ => 1f,
        };
        Modulate = Color.White.WithAlpha(alpha);

        _content.Modulate = Color.White.WithAlpha(Math.Clamp((_openY - 0.45f) / 0.55f, 0f, 1f));

        if (!_openX.Equals(_laidOutX) || !_openY.Equals(_laidOutY))
        {
            _laidOutX = _openX;
            _laidOutY = _openY;
            InvalidateMeasure();
        }

        _frame.Fraction = _phase switch
        {
            Phase.Hold => _holdLeft / _hold,
            Phase.Close or Phase.Done => 0f,
            _ => 1f,
        };
        _frame.Highlight = _hovered;
    }

    private static float Lerp(float a, float b, float t)
    {
        return a + (b - a) * t;
    }

    private static float EaseOut(float t)
    {
        t = Math.Clamp(t, 0f, 1f);
        return 1f - MathF.Pow(1f - t, 3f);
    }

    private static float EaseInOut(float t)
    {
        t = Math.Clamp(t, 0f, 1f);
        return t < 0.5f ? 4f * t * t * t : 1f - MathF.Pow(-2f * t + 2f, 3f) / 2f;
    }

    private static Vector2 MeasureText(string text, Font font)
    {
        var probe = new Label
        {
            Text = text.Length == 0 ? " " : text,
            FontOverride = font,
        };

        probe.Measure(Vector2Helpers.Infinity);
        return probe.DesiredSize;
    }

    private static List<string> Wrap(string text, Font font, float maxWidth)
    {
        var lines = new List<string>();

        foreach (var paragraph in text.Replace("\r", string.Empty).Split('\n'))
        {
            var current = string.Empty;

            foreach (var word in paragraph.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                if (current.Length > 0)
                {
                    var candidate = current + " " + word;
                    if (MeasureText(candidate, font).X <= maxWidth)
                    {
                        current = candidate;
                        continue;
                    }

                    lines.Add(current);
                    current = string.Empty;
                }

                if (MeasureText(word, font).X <= maxWidth)
                {
                    current = word;
                    continue;
                }

                var chunkStart = 0;
                for (var end = chunkStart + 2; end <= word.Length; end++)
                {
                    if (MeasureText(word.Substring(chunkStart, end - chunkStart), font).X <= maxWidth)
                        continue;

                    lines.Add(word.Substring(chunkStart, end - 1 - chunkStart));
                    chunkStart = end - 1;
                }

                current = word.Substring(chunkStart);
            }

            lines.Add(current);
        }

        return lines;
    }

    private sealed class PortraitView : Control
    {
        private const float EntityZoom = 1.45f;
        private const float EntityTopOffset = 0.06f;

        private readonly Texture? _texture;
        private readonly SpriteView? _spriteView;
        private readonly Color _top;
        private readonly Color _bottom;
        private readonly Color _floor;

        public PortraitView(Texture? texture, Color accent, EntityUid? entity = null)
        {
            _texture = texture;
            _top = Color.InterpolateBetween(InsetColor, accent, 0.05f);
            _bottom = Color.InterpolateBetween(InsetColor, accent, 0.42f);
            _floor = accent.WithAlpha(0.8f);
            MouseFilter = MouseFilterMode.Ignore;
            RectClipContent = true;

            if (entity != null)
            {
                _spriteView = new SpriteView
                {
                    OverrideDirection = Direction.South,
                    Stretch = SpriteView.StretchMode.Fill,
                    MouseFilter = MouseFilterMode.Ignore,
                };
                _spriteView.SetEntity(entity.Value);
                AddChild(_spriteView);
            }
        }

        protected override Vector2 MeasureOverride(Vector2 availableSize)
        {
            _spriteView?.Measure(new Vector2(float.PositiveInfinity, float.PositiveInfinity));
            return Vector2.Zero;
        }

        protected override Vector2 ArrangeOverride(Vector2 finalSize)
        {
            if (_spriteView != null)
            {
                var side = MathF.Max(finalSize.X, finalSize.Y) * EntityZoom;
                var x = (finalSize.X - side) / 2f;
                var y = -side * EntityTopOffset;
                _spriteView.Arrange(UIBox2.FromDimensions(new Vector2(x, y), new Vector2(side, side)));
            }

            return finalSize;
        }

        protected override void Draw(DrawingHandleScreen handle)
        {
            base.Draw(handle);

            float w = PixelWidth;
            float h = PixelHeight;
            if (w <= 0f || h <= 0f)
                return;

            var rows = Math.Max(1, PixelHeight / 2);
            for (var i = 0; i < rows; i++)
            {
                var y0 = MathF.Floor(h * i / rows);
                var y1 = MathF.Floor(h * (i + 1) / rows);
                var t = (i + 0.5f) / rows;
                handle.DrawRect(new UIBox2(0f, y0, w, y1), Color.InterpolateBetween(_top, _bottom, t * t));
            }

            var line = MathF.Max(1f, MathF.Round(UIScale));
            handle.DrawRect(new UIBox2(0f, h - line, w, h), _floor);

            if (_texture == null)
                return;

            var size = _texture.Size;
            var room = h - line;
            var fit = MathF.Min(w / Math.Max(1, size.X), room / Math.Max(1, size.Y));
            var scale = fit >= 1f ? MathF.Floor(fit) : fit;

            var drawW = size.X * scale;
            var drawH = size.Y * scale;
            var x = MathF.Floor((w - drawW) / 2f);
            var y = MathF.Floor(room - drawH);

            handle.DrawTextureRect(_texture, UIBox2.FromDimensions(new Vector2(x, y), new Vector2(drawW, drawH)));
        }
    }

    private sealed class FrameOverlay : Control
    {
        private readonly Color _accent;
        private readonly Color _highlight;
        private readonly Color _fill;

        public float Fraction = 1f;
        public bool Highlight;

        public FrameOverlay(Color accent)
        {
            _accent = accent;
            _highlight = Color.InterpolateBetween(BorderColor, accent, 0.6f);
            _fill = accent.WithAlpha(0.85f);
            MouseFilter = MouseFilterMode.Ignore;
        }

        protected override void Draw(DrawingHandleScreen handle)
        {
            base.Draw(handle);

            float w = PixelWidth;
            float h = PixelHeight;
            if (w <= 0f || h <= 0f)
                return;

            var s = MathF.Max(1f, MathF.Round(UIScale));
            var border = Highlight ? _highlight : BorderColor;

            handle.DrawRect(new UIBox2(0f, 0f, w, s), border);
            handle.DrawRect(new UIBox2(0f, h - s, w, h), border);
            handle.DrawRect(new UIBox2(w - s, 0f, w, h), border);
            handle.DrawRect(new UIBox2(0f, 0f, 3f * s, h), _accent);

            var barTop = h - 3f * s;
            if (barTop <= s)
                return;

            var x0 = 3f * s;
            var x1 = w - s;
            handle.DrawRect(new UIBox2(x0, barTop, x1, h - s), InsetColor);

            var fill = MathF.Round((x1 - x0) * Math.Clamp(Fraction, 0f, 1f));
            if (fill > 0f)
                handle.DrawRect(new UIBox2(x0, barTop, x0 + fill, h - s), _fill);
        }
    }
}
