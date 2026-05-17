using Content.Client.Resources;
using Content.Client.Stylesheets;
using Content.Client.Stylesheets.Palette;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Utility;
using static Content.Client.Stylesheets.StylesheetHelpers;
using static Robust.Client.UserInterface.StylesheetHelpers;

namespace Content.Client.Imperial.UI;

/// <summary>
/// Styles for the window pop-out / pop-in header buttons.
/// </summary>
public sealed class ImperialWindowPopOutStyles : StyleBase
{
    public const string StyleClassPopOutButton = "imperialWindowPopOutButton";
    public const string StyleClassPopInButton = "imperialWindowPopInButton";

    public static readonly ResPath PopOutIconPath =
        new("/Textures/Imperial/Interface/Misc/window_arrow/arrow-up-right.svg.96dpi.png");
    public static readonly ResPath PopInIconPath =
        new("/Textures/Imperial/Interface/Misc/window_arrow/arrow-down-left.svg.96dpi.png");

    public override Stylesheet Stylesheet { get; }

    public ImperialWindowPopOutStyles(IResourceCache resCache) : base(resCache)
    {
        var popOutTex = resCache.GetTexture(PopOutIconPath);
        var popInTex = resCache.GetTexture(PopInIconPath);

        Stylesheet = new Stylesheet([
            E<TextureButton>()
                .Class(StyleClassPopOutButton)
                .Prop(TextureButton.StylePropertyTexture, popOutTex)
                .Margin(3),
            E<TextureButton>()
                .Class(StyleClassPopOutButton)
                .PseudoNormal()
                .Modulate(Palettes.Neutral.Element),
            E<TextureButton>()
                .Class(StyleClassPopOutButton)
                .PseudoHovered()
                .Modulate(Palettes.Cyan.HoveredElement),
            E<TextureButton>()
                .Class(StyleClassPopOutButton)
                .PseudoPressed()
                .Modulate(Palettes.Cyan.PressedElement),

            E<TextureButton>()
                .Class(StyleClassPopInButton)
                .Prop(TextureButton.StylePropertyTexture, popInTex)
                .Margin(3),
            E<TextureButton>()
                .Class(StyleClassPopInButton)
                .PseudoNormal()
                .Modulate(Palettes.Neutral.Element),
            E<TextureButton>()
                .Class(StyleClassPopInButton)
                .PseudoHovered()
                .Modulate(Palettes.Cyan.HoveredElement),
            E<TextureButton>()
                .Class(StyleClassPopInButton)
                .PseudoPressed()
                .Modulate(Palettes.Cyan.PressedElement),
        ]);
    }
}
