using Content.Client.Resources;
using Content.Client.Stylesheets;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using static Robust.Client.UserInterface.StylesheetHelpers;

namespace Content.Client.Imperial.UI;


public sealed class ImperialScrollbars : StyleBase
{
    public const string StyleClassScrollbarMedium = "ImperialScrollbarMedium";


    public override Stylesheet Stylesheet { get; }


    public ImperialScrollbars(IResourceCache resCache) : base(resCache)
    {
        Stylesheet = new Stylesheet([
            Element<VScrollBar>().Class(StyleClassScrollbarMedium)
                .Prop(
                    ScrollBar.StylePropertyGrabber,
                    new StyleBoxFlat
                    {
                        BackgroundColor = Color.FromHex("#303030"),
                        ContentMarginTopOverride = 2,
                        ContentMarginLeftOverride = 7,
                    }
                )
        ]);
    }
}
