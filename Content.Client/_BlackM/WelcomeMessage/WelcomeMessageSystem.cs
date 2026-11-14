using System.Numerics;
using Content.Client.Resources;
using Content.Client.UserInterface.Controls;
using Content.Shared._BlackM.WelcomeMessage;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;

namespace Content.Client._BlackM.WelcomeMessage;

public sealed class WelcomeMessageSystem : EntitySystem
{
    [Dependency] private readonly IUserInterfaceManager _ui = default!;
    [Dependency] private readonly IResourceCache _res = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    private static readonly string[] FontStack =
    {
        "/Fonts/_BlackM/Bubble/Grand9KPixelCyrillic.ttf",
        "/Fonts/NotoSans/NotoSans-Regular.ttf",
    };

    private static readonly string[] SoundAppear =
    {
        "/Audio/_BlackM/Radio/receive_1.ogg",
        "/Audio/_BlackM/Radio/receive_2.ogg",
        "/Audio/_BlackM/Radio/receive_3.ogg",
    };

    private static readonly string[] SoundDisappear =
    {
        "/Audio/_BlackM/Radio/send_end_1.ogg",
        "/Audio/_BlackM/Radio/send_end_2.ogg",
        "/Audio/_BlackM/Radio/send_end_3.ogg",
    };

    private static readonly string[] SoundType =
    {
        "/Audio/_BlackM/Interface/Terminal/term_type.ogg",
    };

    private const float AppearVolume = -5f;
    private const float DisappearVolume = -7f;
    private const float TypeVolume = -16f;

    private readonly Random _rng = new();

    private WelcomeMessageControl? _current;
    private WelcomeFonts? _fonts;

    public bool IsShowing => _current != null;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeNetworkEvent<WelcomeMessageEvent>(OnMessage);
    }

    public WelcomeFonts GetFonts()
    {
        return _fonts ??= new WelcomeFonts(
            _res.GetFont(FontStack, 11),
            _res.GetFont(FontStack, 14),
            _res.GetFont(FontStack, 12));
    }

    private void OnMessage(WelcomeMessageEvent ev)
    {
        _current?.Orphan();
        _current = null;

        var control = new WelcomeMessageControl(
            ev.Sender,
            ev.Title,
            ev.Text,
            GetPortrait(ev),
            ev.Accent,
            ev.Duration,
            GetFonts());

        control.PlaySound += PlayWelcomeSound;

        var parent = GetParent();
        parent.AddChild(control);

        if (parent is LayoutContainer)
        {
            LayoutContainer.SetAnchorPreset(control, LayoutContainer.LayoutPreset.CenterTop);
            LayoutContainer.SetGrowHorizontal(control, LayoutContainer.GrowDirection.Both);
            LayoutContainer.SetMarginLeft(control, 0);
            LayoutContainer.SetMarginRight(control, 0);
            LayoutContainer.SetMarginTop(control, WelcomeMessageControl.TopMargin);
            LayoutContainer.SetMarginBottom(control, WelcomeMessageControl.TopMargin);
        }

        _current = control;
    }

    public Control GetParent()
    {
        var screen = _ui.ActiveScreen;
        if (screen != null &&
            screen.TryGetWidget<MainViewport>(out var viewport) &&
            viewport.Parent is LayoutContainer container)
        {
            return container;
        }

        return (Control?) screen ?? _ui.PopupRoot;
    }

    private Texture? GetPortrait(WelcomeMessageEvent ev)
    {
        var texture = _sprite.Frame0(ev.Portrait);
        var size = texture.Size;

        if (ev.CropSize.X <= 0 || ev.CropSize.Y <= 0)
            return texture;

        var x = Math.Clamp(ev.CropPos.X, 0, Math.Max(0, size.X - 1));
        var y = Math.Clamp(ev.CropPos.Y, 0, Math.Max(0, size.Y - 1));
        var w = Math.Min(ev.CropSize.X, size.X - x);
        var h = Math.Min(ev.CropSize.Y, size.Y - y);

        if (w <= 0 || h <= 0)
            return texture;

        return new AtlasTexture(texture, UIBox2.FromDimensions(new Vector2(x, y), new Vector2(w, h)));
    }

    public void PlayWelcomeSound(WelcomeSound kind)
    {
        string path;
        float volume;
        var pitch = 1f;

        switch (kind)
        {
            case WelcomeSound.Appear:
                path = SoundAppear[_rng.Next(SoundAppear.Length)];
                volume = AppearVolume;
                break;
            case WelcomeSound.Disappear:
                path = SoundDisappear[_rng.Next(SoundDisappear.Length)];
                volume = DisappearVolume;
                break;
            default:
                path = SoundType[_rng.Next(SoundType.Length)];
                volume = TypeVolume;
                pitch = 0.92f + (float) _rng.NextDouble() * 0.16f;
                break;
        }

        _audio.PlayGlobal(path, Filter.Local(), false,
            AudioParams.Default.WithVolume(volume).WithPitchScale(pitch));
    }

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        if (_current is { Finished: true })
        {
            _current.Orphan();
            _current = null;
        }
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _current?.Orphan();
        _current = null;
    }
}
