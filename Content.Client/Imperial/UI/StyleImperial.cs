using System.Linq;
using System.Numerics;
using Content.Client.Resources;
using Content.Client.Stylesheets;
using Content.Client.UserInterface.Controls;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using static Robust.Client.UserInterface.StylesheetHelpers;

namespace Content.Client.Imperial.UI;


public sealed class StyleImperial : StyleBase
{
    // Menu
    public const string StyleclassButtonOpenLeftMenu = "ImperialButtonOpenLeftMenu";

    // Stripe
    public const string StyleClassStripePass = "ImperialStripePass";
    public const string StyleClassStripePassPlus = "ImperialStripePassPlus";
    public const string StyleClassStripeUltra = "ImperialStripeUltra";


    // ____________________________________________________ //

    #region Buttons

    public const string StyleClassRoundedButton = "ImperialRoundedButton";

    #endregion

    // ____________________________________________________ //

    #region Colors

    public static Color ImperialDark = Color.FromHex("#111111");

    public static Color ImperialGrey = Color.FromHex("#1F1F1F");

    // Imperial Pass Begin
    public static readonly Color ButtonColorDefaultPass = Color.FromHex("#464966");
    public static readonly Color ButtonColorHoveredPass = Color.FromHex("#575b7f");
    public static readonly Color ButtonColorPressedPass = Color.FromHex("#3e6c45");
    // Imperial Pass End

    #endregion

    public override Stylesheet Stylesheet { get; }

    public StyleImperial(IResourceCache resCache) : base(resCache)
    {
        var notoSans10 = resCache.NotoStack(size: 10);

        var imperialRoundedButtonTexture = resCache.GetTexture("/Textures/Imperial/ImperialPass/Nano/rounded_button.svg.96dpi.png");
        var imperialRoundedButton = new StyleBoxTexture
        {
            Texture = imperialRoundedButtonTexture,
        };
        imperialRoundedButton.SetPatchMargin(StyleBox.Margin.All, 5);
        imperialRoundedButton.SetPadding(StyleBox.Margin.All, 2);

        var pwindowHeaderTex = resCache.GetTexture("/Textures/Imperial/Pass/window/window_header.png");
        var pwindowHeader = new StyleBoxTexture
        {
            Texture = pwindowHeaderTex,
            PatchMarginBottom = 3,
            ExpandMarginBottom = 3,
            ContentMarginBottomOverride = 0
        };
        var pwindowBackgroundTex = resCache.GetTexture("/Textures/Imperial/Pass/window/window_background.png");
        var pwindowBackground = new StyleBoxTexture
        {
            Texture = pwindowBackgroundTex,
        };
        pwindowBackground.SetPatchMargin(StyleBox.Margin.Horizontal | StyleBox.Margin.Bottom, 2);
        pwindowBackground.SetExpandMargin(StyleBox.Margin.Horizontal | StyleBox.Margin.Bottom, 2);
        var pwindowBackgroundTexr = resCache.GetTexture("/Textures/Imperial/Pass/window/window_backgroundr.png");
        var pwindowBackgroundr = new StyleBoxTexture
        {
            Texture = pwindowBackgroundTexr,
        };
        pwindowBackgroundr.SetPatchMargin(StyleBox.Margin.Horizontal | StyleBox.Margin.Bottom, 2);
        pwindowBackgroundr.SetExpandMargin(StyleBox.Margin.Horizontal | StyleBox.Margin.Bottom, 2);

        var passrect = new StyleBoxTexture
        {
            Texture = resCache.GetTexture("/Textures/Interface/Nano/square_black.png"),
        };
        passrect.SetPatchMargin(StyleBox.Margin.All, 10);

        var plineEditTex = resCache.GetTexture("/Textures/Imperial/Pass/window/lineedit.png");
        var plineEdit = new StyleBoxTexture
        {
            Texture = plineEditTex,
        };
        plineEdit.SetPatchMargin(StyleBox.Margin.All, 3);
        plineEdit.SetContentMarginOverride(StyleBox.Margin.Horizontal, 5);

        var stripeBackTex2 = resCache.GetTexture("/Textures/Imperial/Pass/stripeback.svg.96dpi.png");
        var stripeBackpass = new StyleBoxTexture
        {
            Texture = stripeBackTex2,
            Mode = StyleBoxTexture.StretchMode.Tile
        };
        // Menu

        var buttonTex = resCache.GetTexture("/Textures/Interface/Nano/button.svg.96dpi.png");
        var topButtonBase = new StyleBoxTexture
        {
            Texture = buttonTex,
        };
        topButtonBase.SetPatchMargin(StyleBox.Margin.All, 10);
        topButtonBase.SetPadding(StyleBox.Margin.All, 0);
        topButtonBase.SetContentMarginOverride(StyleBox.Margin.All, 0);

        var topButtonOpenLeft = new StyleBoxTexture(topButtonBase)
        {
            Texture = new AtlasTexture(buttonTex, UIBox2.FromDimensions(new Vector2(10, 0), new Vector2(14, 24))),
        };
        topButtonOpenLeft.SetPatchMargin(StyleBox.Margin.Left, 0);


        // Stripe

        var stripePassTexture = resCache.GetTexture("/Textures/Imperial/ImperialPass/Nano/stripe/pass_stripe.svg.96dpi.png");
        var stripePass = new StyleBoxTexture
        {
            Texture = stripePassTexture,
            Mode = StyleBoxTexture.StretchMode.Tile
        };

        var stripeUltraTexture = resCache.GetTexture("/Textures/Imperial/ImperialPass/Nano/stripe/ultra_stripe.svg.96dpi.png");
        var stripeUltra = new StyleBoxTexture
        {
            Texture = stripeUltraTexture,
            Mode = StyleBoxTexture.StretchMode.Tile
        };

        var rules = BaseRules.Concat([
            new StyleRule(new SelectorElement(typeof(Button), new[] {StyleClassRoundedButton}, null, null), new[]
            {
                new StyleProperty(Button.StylePropertyStyleBox, imperialRoundedButton),
            }),

            // Stripe

            new StyleRule(new SelectorElement(typeof(StripeBack), new[] { StyleClassStripePass }, null, null), new[]
            {
                new StyleProperty(StripeBack.StylePropertyBackground, stripePass),
            }),

            new StyleRule(new SelectorElement(typeof(StripeBack), new[] { StyleClassStripeUltra }, null, null), new[]
            {
                new StyleProperty(StripeBack.StylePropertyBackground, stripeUltra),
            }),

            // Some shit

            new StyleRule(
                new SelectorElement(null, new[] {"windowPanelPassReconnect"}, null, null),
                new[]
                {
                    new StyleProperty(PanelContainer.StylePropertyPanel, pwindowBackgroundr),
                }),

            // Imperial Pass Begin
            new StyleRule(new SelectorElement(typeof(Label), new[] {"LabelSubTextPassExtraBoldServerName"}, null, null), new[]
            {
                new StyleProperty(Label.StylePropertyFont, resCache.GetFont("/Fonts/Imperial/Pass/SourceCodePro-SemiBold.ttf", 14)),
                new StyleProperty(Label.StylePropertyFontColor, Color.FromHex("#FFFFFF"))
            }),
            new StyleRule(new SelectorElement(typeof(Label), new[] {"LabelSubTextPassPlayerCount"}, null, null), new[]
            {
                new StyleProperty(Label.StylePropertyFont, resCache.GetFont("/Fonts/Imperial/Pass/SourceCodePro-SemiBold.ttf", 12)),
                new StyleProperty(Label.StylePropertyFontColor, Color.FromHex("#FFFFFF"))
            }),
            new StyleRule(new SelectorElement(typeof(Label), new[] {"LabelSubTextPassRecommend"}, null, null), new[]
            {
                new StyleProperty(Label.StylePropertyFont, resCache.GetFont("/Fonts/Imperial/Pass/SourceCodePro-SemiBold.ttf", 10)),
                new StyleProperty(Label.StylePropertyFontColor, Color.FromHex("#FFFFFF"))
            }),
            new StyleRule(new SelectorElement(typeof(Label), new[] {"LabelSubTextPass1"}, null, null), new[]
            {
                new StyleProperty(Label.StylePropertyFont, resCache.GetFont("/Fonts/Imperial/Pass/SourceCodePro-Bold.ttf", 14)),
                new StyleProperty(Label.StylePropertyFontColor, Color.FromHex("#FFFFFF"))
            }),
            // Imperial reconnect start
            new StyleRule(new SelectorElement(typeof(RichTextLabel), new[] {"LabelSubTextPass1"}, null, null), new[]
            {
                new StyleProperty(Label.StylePropertyFont, resCache.GetFont("/Fonts/Imperial/Pass/SourceCodePro-Bold.ttf", 14))
            }),
            new StyleRule(new SelectorElement(typeof(Label), new[] {"LabelSubTextPass20"}, null, null), new[]
            {
                new StyleProperty(Label.StylePropertyFont, resCache.GetFont("/Fonts/Imperial/Pass/SourceCodePro-Bold.ttf", 15)),
                new StyleProperty(Label.StylePropertyFontColor, Color.FromHex("#FFFFFF"))
            }),
            new StyleRule(new SelectorElement(typeof(ScrollContainer), new[] {"ScrollContainerReconnectWindow"}, null, null), new[]
            {
                new StyleProperty(ScrollContainer.StylePropertyModulateSelf, Color.FromHex("#999999")),
            }),
            new StyleRule(new SelectorElement(typeof(VScrollBar), new[] {"imperialscroll"}, null, null),
                new[]
                {
                    new StyleProperty(ScrollBar.StylePropertyGrabber,
                        new StyleBoxFlat
                        {
                            BackgroundColor = Color.FromHex("#303030"), ContentMarginTopOverride = 2, ContentMarginLeftOverride = 6
                        }),
                }),

            new StyleRule(
                new SelectorElement(typeof(VScrollBar), new[] {"imperialscroll"}, null, new[] {ScrollBar.StylePseudoClassHover}),
                new[]
                {
                    new StyleProperty(ScrollBar.StylePropertyGrabber,
                        new StyleBoxFlat
                        {
                            BackgroundColor = Color.FromHex("#303030"), ContentMarginTopOverride = 2, ContentMarginLeftOverride = 8
                        }),
                }),

            new StyleRule(
                new SelectorElement(typeof(VScrollBar), new[] {"imperialscroll"}, null, new[] {ScrollBar.StylePseudoClassGrabbed}),
                new[]
                {
                    new StyleProperty(ScrollBar.StylePropertyGrabber,
                        new StyleBoxFlat
                        {
                            BackgroundColor = Color.FromHex("#303030"), ContentMarginTopOverride = 2, ContentMarginLeftOverride = 8
                        }),
                }),
            // Imperial reconnect start
            new StyleRule(new SelectorElement(typeof(Label), new[] {"LabelSubTextPassUpper"}, null, null), new[]
            {
                new StyleProperty(Label.StylePropertyFont, resCache.GetFont("/Fonts/Imperial/Pass/SourceCodePro-SemiBold.ttf", 14)),
                new StyleProperty(Label.StylePropertyFontColor, Color.FromHex("#FFFFFF"))
            }),
            new StyleRule(new SelectorElement(typeof(RichTextLabel), new[] {"LabelSubTextPassDescription"}, null, null), new[]
            {
                new StyleProperty(Label.StylePropertyFont, resCache.GetFont("/Fonts/Imperial/Pass/SourceCodePro-Medium.ttf", 11)),
                new StyleProperty(Label.StylePropertyFontColor, Color.FromHex("#FFFFFF"))
            }),
            new StyleRule(new SelectorElement(typeof(RichTextLabel), new[] {"LabelSubTextPassExtraBoldNickname"}, null, null), new[]
            {
                new StyleProperty(Label.StylePropertyFont, resCache.GetFont("/Fonts/Imperial/Pass/SourceCodePro-ExtraBold.ttf", 11)),
                new StyleProperty(Label.StylePropertyFontColor, Color.FromHex("#7E7E7E"))
            }),
            new StyleRule(new SelectorElement(typeof(Label), new[] {"LabelSubTextPassUpperBlue"}, null, null), new[]
            {
                new StyleProperty(Label.StylePropertyFont, resCache.GetFont("/Fonts/Imperial/Pass/SourceCodePro-SemiBold.ttf", 14)),
                new StyleProperty(Label.StylePropertyFontColor, Color.FromHex("#58C4E6"))
            }),
            new StyleRule(new SelectorElement(typeof(Label), new[] {"LabelSubTextPassGift"}, null, null), new[]
            {
                new StyleProperty(Label.StylePropertyFont, resCache.GetFont("/Fonts/Imperial/Pass/SourceCodePro-Medium.ttf", 11)),
                new StyleProperty(Label.StylePropertyFontColor, Color.FromHex("#FFFFFF"))
            }),
            new StyleRule(new SelectorElement(typeof(Label), new[] {"LabelSubTextPassLower"}, null, null), new[]
            {
                new StyleProperty(Label.StylePropertyFont, resCache.GetFont("/Fonts/Imperial/Pass/SourceCodePro-SemiBold.ttf", 10)),
                new StyleProperty(Label.StylePropertyFontColor, Color.FromHex("#FFFFFF"))
            }),
            new StyleRule(new SelectorElement(typeof(Label), new[] {"LabelSubTextPassLowerTTS"}, null, null), new[]
            {
                new StyleProperty(Label.StylePropertyFont, resCache.GetFont("/Fonts/Imperial/Pass/SourceCodePro-SemiBold.ttf", 14)),
                new StyleProperty(Label.StylePropertyFontColor, Color.FromHex("#FFFFFF"))
            }),
            new StyleRule(new SelectorElement(typeof(Label), new[] {"LabelSubTextPassLowerBlue"}, null, null), new[]
            {
                new StyleProperty(Label.StylePropertyFont, resCache.GetFont("/Fonts/Imperial/Pass/SourceCodePro-SemiBold.ttf", 10)),
                new StyleProperty(Label.StylePropertyFontColor, Color.FromHex("#58C4E6"))
            }),
            new StyleRule(new SelectorElement(typeof(Label), new[] {"LabelSubTextPassMedium"}, null, null), new[]
            {
                new StyleProperty(Label.StylePropertyFont, resCache.GetFont("/Fonts/Imperial/Pass/SourceCodePro-Medium.ttf", 9)),
                new StyleProperty(Label.StylePropertyFontColor, Color.FromHex("#FFFFFF"))
            }),
            new StyleRule(new SelectorElement(typeof(RichTextLabel), new[] {"LabelSubTextPassLowerLow"}, null, null), new[]
            {
                new StyleProperty(Label.StylePropertyFont, resCache.GetFont("/Fonts/Imperial/Pass/SourceCodePro-Medium.ttf", 8)),
                new StyleProperty(Label.StylePropertyFontColor, Color.FromHex("#FFFFFF"))
            }),
            new StyleRule(new SelectorElement(typeof(RichTextLabel), new[] {"LabelSubTextPassMedium"}, null, null), new[]
            {
                new StyleProperty(Label.StylePropertyFont, resCache.GetFont("/Fonts/Imperial/Pass/SourceCodePro-Medium.ttf", 9))
            }),
            new StyleRule(new SelectorElement(typeof(RichTextLabel), new[] {"LabelSubTextPassMediumBold"}, null, null), new[]
            {
                new StyleProperty(Label.StylePropertyFont, resCache.GetFont("/Fonts/Imperial/Pass/SourceCodePro-Bold.ttf", 9))
            }),
            new StyleRule(new SelectorElement(typeof(Label), new[] {"LabelSubTextPassMediumBuy"}, null, null), new[]
            {
                new StyleProperty(Label.StylePropertyFont, resCache.GetFont("/Fonts/Imperial/Pass/SourceCodePro-Medium.ttf", 10)),
                new StyleProperty(Label.StylePropertyFontColor, Color.FromHex("#FFFFFF"))
            }),
            new StyleRule(new SelectorElement(typeof(RichTextLabel), new[] {"LabelSubTextPassMediumBuy"}, null, null), new[]
            {
                new StyleProperty(Label.StylePropertyFont, resCache.GetFont("/Fonts/Imperial/Pass/SourceCodePro-Medium.ttf", 10))
            }),
            new StyleRule(new SelectorElement(typeof(RichTextLabel), new[] {"LabelSubTextPassBoldBuy"}, null, null), new[]
            {
                new StyleProperty(Label.StylePropertyFont, resCache.GetFont("/Fonts/Imperial/Pass/SourceCodePro-Bold.ttf", 10))
            }),
            new StyleRule(new SelectorElement(typeof(RichTextLabel), new[] {"LabelSubTextPassPlayerCount"}, null, null), new[]
            {
                new StyleProperty(Label.StylePropertyFont, resCache.GetFont("/Fonts/Imperial/Pass/SourceCodePro-SemiBold.ttf", 12))
            }),
            new StyleRule(new SelectorElement(typeof(RichTextLabel), new[] {"LabelSubTextPassRecommend"}, null, null), new[]
            {
                new StyleProperty(Label.StylePropertyFont, resCache.GetFont("/Fonts/Imperial/Pass/SourceCodePro-SemiBold.ttf", 10))
            }),
            new StyleRule(new SelectorElement(typeof(Label), new[] {"LabelSubTextPassMedium20"}, null, null), new[]
            {
                new StyleProperty(Label.StylePropertyFont, resCache.GetFont("/Fonts/Imperial/Pass/SourceCodePro-Medium.ttf", 10)),
                new StyleProperty(Label.StylePropertyFontColor, Color.FromHex("#FFFFFF"))
            }),
            new StyleRule(new SelectorElement(typeof(Label), new[] {"LabelSubTextPassMediumTTS"}, null, null), new[]
            {
                new StyleProperty(Label.StylePropertyFont, resCache.GetFont("/Fonts/Imperial/Pass/SourceCodePro-Medium.ttf", 12)),
                new StyleProperty(Label.StylePropertyFontColor, Color.FromHex("#FFFFFF"))
            }),
            new StyleRule(new SelectorElement(typeof(RichTextLabel), new[] {"LabelSubTextPassMedium20"}, null, null), new[]
            {
                new StyleProperty(Label.StylePropertyFont, resCache.GetFont("/Fonts/Imperial/Pass/SourceCodePro-Medium.ttf", 10))
            }),
            new StyleRule(new SelectorElement(typeof(RichTextLabel), new[] {"LabelSubTextPassUpper"}, null, null), new[]
            {
                new StyleProperty("font", resCache.GetFont("/Fonts/Imperial/Pass/SourceCodePro-SemiBold.ttf", 14))
                // new StyleProperty(Label.StylePropertyFontColor, Color.FromHex("#FFFFFF"))
            }),
            new StyleRule(new SelectorElement(typeof(RichTextLabel), new[] {"LabelSubTextPassLower"}, null, null), new[]
            {
                new StyleProperty("font", resCache.GetFont("/Fonts/Imperial/Pass/SourceCodePro-SemiBold.ttf", 10))
                //new StyleProperty(Label.StylePropertyFontColor, Color.FromHex("#FFFFFF"))
            }),
            new StyleRule(new SelectorElement(typeof(RichTextLabel), new[] {"LabelSubTextPassFreePass"}, null, null), new[]
            {
                new StyleProperty("font", resCache.GetFont("/Fonts/Imperial/Pass/SourceCodePro-Bold.ttf", 14))
            }),
            new StyleRule(new SelectorElement(typeof(Content.Client.Imperial.UI.ImperialLineEdit), null, null, null),
                new[]
                {
                    new StyleProperty(Content.Client.Imperial.UI.ImperialLineEdit.StylePropertyStyleBox, plineEdit),
                }),

            new StyleRule(
                new SelectorElement(typeof(Content.Client.Imperial.UI.ImperialLineEdit), new[] {Content.Client.Imperial.UI.ImperialLineEdit.StyleClassLineEditNotEditable}, null, null),
                new[]
                {
                    new StyleProperty("font", resCache.GetFont("/Fonts/Imperial/Pass/SourceCodePro-Medium.ttf", 12)),
                    new StyleProperty("font-color", Color.FromHex("#FFFFFF")),
                }),

            new StyleRule(
                new SelectorElement(typeof(Content.Client.Imperial.UI.ImperialLineEdit), null, null, new[] {Content.Client.Imperial.UI.ImperialLineEdit.StylePseudoClassPlaceholder}),
                new[]
                {
                    new StyleProperty("font", resCache.GetFont("/Fonts/Imperial/Pass/SourceCodePro-Medium.ttf", 12)),
                    new StyleProperty("font-color", Color.FromHex("#343434")),
                }),
            // Imperial Pass End

            // Imperial Pass Begin
            new StyleRule(new SelectorChild(
                new SelectorElement(typeof(BoxContainer), new[] {"PassButton"}, null, null),
                new SelectorElement(typeof(Label), null, null, null)),
                new[]
                {
                    new StyleProperty("font", resCache.GetFont("/Fonts/Imperial/Pass/SourceCodePro-Medium.ttf", 10))
                }),
            // Imperial Pass Begin
            Element<PanelContainer>().Class("WindowHeadingBackgroundPass")
                .Prop(PanelContainer.StylePropertyPanel, new StyleBoxFlat(Color.FromHex("#222222"))),

            Element<PanelContainer>().Class("PanelBackgroundPass")
                .Prop(PanelContainer.StylePropertyPanel, new StyleBoxFlat(Color.FromHex("#131313"))),

            Element<PanelContainer>().Class("PanelBackgroundRoadmapCard")
                .Prop(PanelContainer.StylePropertyPanel, new StyleBoxFlat(Color.FromHex("#1F1F1F"))),

            // Imperial Pass End
            // Imperial Roadmap Start
            Element<RichTextLabel>().Class("RoadmapPlanName")
                .Prop(Label.StylePropertyFont, resCache.GetFont("/Fonts/Imperial/Pass/SourceCodePro-Medium.ttf", 15))
                .Prop(Label.StylePropertyFontColor, Color.FromHex("#FFFFFF")),
            Element<RichTextLabel>().Class("RoadmapPlanDescription")
                .Prop(Label.StylePropertyFont, resCache.GetFont("/Fonts/Imperial/Pass/SourceCodePro-Medium.ttf", 10))
                .Prop(Label.StylePropertyFontColor, Color.FromHex("#939393")),
            // Imperial Roadmap End

            // Imperial Pass Begin
            Element<RichTextLabel>().Class("LabelSubTextPass")
                .Prop(Label.StylePropertyFont, resCache.GetFont("/Fonts/Imperial/Pass/SourceCodePro-SemiBold.ttf", 10))
                .Prop(Label.StylePropertyFontColor, Color.FromHex("#FFFFFF")),

            Element<RichTextLabel>().Class("LabelSubTextPass1")
                .Prop(Label.StylePropertyFont, resCache.GetFont("/Fonts/Imperial/Pass/SourceCodePro-Bold.ttf", 20))
                .Prop(Label.StylePropertyFontColor, Color.FromHex("#FFFFFF")),

            Element<RichTextLabel>().Class("LabelSubTextPassUpper")
                .Prop(Label.StylePropertyFont, resCache.GetFont("/Fonts/Imperial/Pass/SourceCodePro-SemiBold.ttf", 14))
                .Prop(Label.StylePropertyFontColor, Color.FromHex("#FFFFFF")),

            Element<Label>().Class("ImperialLabelH3")
                .Prop(Label.StylePropertyFont, resCache.GetFont("/Fonts/Imperial/Pass/SourceCodePro-Medium.ttf", 14))
                .Prop(Label.StylePropertyFontColor, Color.FromHex("#FFFFFF")),

             Element<RichTextLabel>().Class("ImperialLabelH3")
                .Prop(Label.StylePropertyFont, resCache.GetFont("/Fonts/Imperial/Pass/SourceCodePro-Medium.ttf", 14))
                .Prop(Label.StylePropertyFontColor, Color.FromHex("#FFFFFF")),

            Element<RichTextLabel>().Class("LabelSubTextPassLower")
                .Prop(Label.StylePropertyFont, resCache.GetFont("/Fonts/Imperial/Pass/SourceCodePro-SemiBold.ttf", 10))
                .Prop(Label.StylePropertyFontColor, Color.FromHex("#FFFFFF")),

            Element<RichTextLabel>().Class("LabelReconnectWindow30")
                .Prop(Label.StylePropertyFont, resCache.GetFont("/Fonts/Imperial/Pass/SourceCodePro-SemiBold.ttf", 30))
                .Prop(Label.StylePropertyFontColor, Color.FromHex("#FFFFFF")),

            Element<Label>().Class("ImperialLabelH1")
                .Prop(Label.StylePropertyFont, resCache.GetFont("/Fonts/Imperial/Pass/SourceCodePro-Bold.ttf", 47))
                .Prop(Label.StylePropertyFontColor, Color.FromHex("#FFFFFF")),

            Element<Label>().Class("ImperialLabelH2")
                .Prop(Label.StylePropertyFont, resCache.GetFont("/Fonts/Imperial/Pass/SourceCodePro-Bold.ttf", 32))
                .Prop(Label.StylePropertyFontColor, Color.FromHex("#FFFFFF")),

            Element<Label>().Class("ImperialLabelGreyH3")
                .Prop(Label.StylePropertyFont, resCache.GetFont("/Fonts/Imperial/Pass/SourceCodePro-Medium.ttf", 14))
                .Prop(Label.StylePropertyFontColor, Color.FromHex("#7E7E7E")),

             Element<RichTextLabel>().Class("ImperialLabelGreyH3")
                .Prop(Label.StylePropertyFont, resCache.GetFont("/Fonts/Imperial/Pass/SourceCodePro-Medium.ttf", 14))
                .Prop(Label.StylePropertyFontColor, Color.FromHex("#7E7E7E")),

            Element<Label>().Class("ImperialLabelGreyH4")
                .Prop(Label.StylePropertyFont, resCache.GetFont("/Fonts/Imperial/Pass/SourceCodePro-Medium.ttf", 9))
                .Prop(Label.StylePropertyFontColor, Color.FromHex("#7E7E7E")),

            Element<RichTextLabel>().Class("ImperialLabelGreyH4")
                .Prop(Label.StylePropertyFont, resCache.GetFont("/Fonts/Imperial/Pass/SourceCodePro-Medium.ttf", 9))
                .Prop(Label.StylePropertyFontColor, Color.FromHex("#7E7E7E")),

            Element<Label>().Class("ImperialLabelH1")
                .Prop(Label.StylePropertyFont, resCache.GetFont("/Fonts/Imperial/Pass/SourceCodePro-Bold.ttf", 47))
                .Prop(Label.StylePropertyFontColor, Color.FromHex("#FFFFFF")),

            Element<Label>().Class("ImperialLabelGreyH4")
                .Prop(Label.StylePropertyFont, resCache.GetFont("/Fonts/Imperial/Pass/SourceCodePro-Medium.ttf", 9))
                .Prop(Label.StylePropertyFontColor, Color.FromHex("#7E7E7E")),
            // Imperial Pass End

            // Imperial Pass Begin
            Element<PanelContainer>().Class("AngleRectPass")
                .Prop(PanelContainer.StylePropertyPanel, passrect)
                .Prop(PanelContainer.StylePropertyPanel, new StyleBoxFlat(Color.FromHex("#222222"))),
            // Imperial Pass End

            Element<BoxContainer>()
                .Class("imperialExam")
                .Prop(StripeBack.StylePropertyBackground,
                    new StyleBoxTexture
                    {
                        Texture = resCache.GetTexture("/Textures/Imperial/Interface/Style/exam_stripeback.svg.96dpi.png"),
                        Mode = StyleBoxTexture.StretchMode.Tile
                    }
                ),

            Element<TextButton>()
                .Class("LabelSubText")
                .Prop(Label.StylePropertyFont, notoSans10)
                .Prop(Label.StylePropertyFontColor, Color.DarkGray)
        ]).ToList();

        rules.AddRange(new LabelsH(resCache).Stylesheet.Rules); // Labels
        rules.AddRange(new ImperialScrollbars(resCache).Stylesheet.Rules); // Scroll bars

        Stylesheet = new Stylesheet(rules);
    }
}
