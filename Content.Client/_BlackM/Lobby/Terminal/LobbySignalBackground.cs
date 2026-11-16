using System.Numerics;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Maths;
using Robust.Shared.Utility;
using Robust.Shared.Timing;

namespace Content.Client._BlackM.Lobby.Terminal;

public sealed class LobbySignalBackground : Control
{
    private const string SpritePath = "/Textures/_BlackM/LobbyScreens/signal_logo.rsi";
    private const int FrameSize = 96;
    private const float SpriteScale = 3f;
    private const float RightPanelShare = 0.26f;
    private const float TopShare = 0.14f;
    private const float BottomShare = 0.04f;

    private readonly AnimatedTextureRect _sprite;

    public LobbySignalBackground()
    {
        MouseFilter = MouseFilterMode.Ignore;

        _sprite = new AnimatedTextureRect
        {
            HorizontalAlignment = HAlignment.Left,
            VerticalAlignment = VAlignment.Top,
            MinSize = new Vector2(FrameSize * SpriteScale),
            ModulateSelfOverride = Color.FromHex("#FFFFFF30"),
            MouseFilter = MouseFilterMode.Ignore,
        };
        _sprite.DisplayRect.TextureScale = new Vector2(SpriteScale);
        _sprite.DisplayRect.Stretch = TextureRect.StretchMode.KeepCentered;
        _sprite.SetFromSpriteSpecifier(new SpriteSpecifier.Rsi(new ResPath(SpritePath), "searching"));
        AddChild(_sprite);
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        var centerX = Size.X * (1f - RightPanelShare) * 0.5f;
        var centerY = Size.Y * (TopShare + (1f - BottomShare)) * 0.5f;
        _sprite.Margin = new Thickness(
            centerX - FrameSize * SpriteScale * 0.5f,
            centerY - FrameSize * SpriteScale * 0.5f,
            0,
            0);
    }
}
