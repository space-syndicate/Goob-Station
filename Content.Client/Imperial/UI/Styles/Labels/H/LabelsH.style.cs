using Content.Client.Resources;
using Content.Client.Stylesheets;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using static Robust.Client.UserInterface.StylesheetHelpers;

namespace Content.Client.Imperial.UI;


public sealed class LabelsH : StyleBase
{
    public const string StyleClassBoldLabelH1 = "LabelCodeBoldH1";
    public const string StyleClassBoldLabelH2 = "LabelCodeBoldH2";
    public const string StyleClassBoldLabelH3 = "LabelCodeBoldH3";
    public const string StyleClassBoldLabelH4 = "LabelCodeBoldH4";

    public const string StyleClassMediumLabelH1 = "LabelCodeMediumH1";
    public const string StyleClassMediumLabelH2 = "LabelCodeMediumH2";
    public const string StyleClassMediumLabelH3 = "LabelCodeMediumH3";
    public const string StyleClassMediumLabelH4 = "LabelCodeMediumH4";

    public const string StyleClassSemiBoldLabelH1 = "LabelCodeSemiBoldH1";
    public const string StyleClassSemiBoldLabelH2 = "LabelCodeSemiBoldH2";
    public const string StyleClassSemiBoldLabelH3 = "LabelCodeSemiBoldH3";
    public const string StyleClassSemiBoldLabelH4 = "LabelCodeSemiBoldH4";


    public override Stylesheet Stylesheet { get; }


    public LabelsH(IResourceCache resCache) : base(resCache)
    {
        Stylesheet = new Stylesheet([
            // __________ Labels __________

            // Bold

            Element<Label>().Class(StyleClassBoldLabelH1)
                .Prop(Label.StylePropertyFont, resCache.GetFont("/Fonts/Imperial/Pass/SourceCodePro-Bold.ttf", 22))
                .Prop(Label.StylePropertyFontColor, Color.FromHex("#FFFFFF")),

            Element<Label>().Class(StyleClassBoldLabelH2)
                .Prop(Label.StylePropertyFont, resCache.GetFont("/Fonts/Imperial/Pass/SourceCodePro-Bold.ttf", 18))
                .Prop(Label.StylePropertyFontColor, Color.FromHex("#FFFFFF")),

            Element<Label>().Class(StyleClassBoldLabelH3)
                .Prop(Label.StylePropertyFont, resCache.GetFont("/Fonts/Imperial/Pass/SourceCodePro-Bold.ttf", 12))
                .Prop(Label.StylePropertyFontColor, Color.FromHex("#FFFFFF")),

            Element<Label>().Class(StyleClassBoldLabelH4)
                .Prop(Label.StylePropertyFont, resCache.GetFont("/Fonts/Imperial/Pass/SourceCodePro-Bold.ttf", 10))
                .Prop(Label.StylePropertyFontColor, Color.FromHex("#FFFFFF")),

            // Medium

            Element<Label>().Class(StyleClassMediumLabelH1)
                .Prop(Label.StylePropertyFont, resCache.GetFont("/Fonts/Imperial/Pass/SourceCodePro-Medium.ttf", 22))
                .Prop(Label.StylePropertyFontColor, Color.FromHex("#FFFFFF")),

            Element<Label>().Class(StyleClassMediumLabelH2)
                .Prop(Label.StylePropertyFont, resCache.GetFont("/Fonts/Imperial/Pass/SourceCodePro-Medium.ttf", 18))
                .Prop(Label.StylePropertyFontColor, Color.FromHex("#FFFFFF")),

            Element<Label>().Class(StyleClassMediumLabelH3)
                .Prop(Label.StylePropertyFont, resCache.GetFont("/Fonts/Imperial/Pass/SourceCodePro-Medium.ttf", 12))
                .Prop(Label.StylePropertyFontColor, Color.FromHex("#FFFFFF")),

            Element<Label>().Class(StyleClassMediumLabelH4)
                .Prop(Label.StylePropertyFont, resCache.GetFont("/Fonts/Imperial/Pass/SourceCodePro-Medium.ttf", 10))
                .Prop(Label.StylePropertyFontColor, Color.FromHex("#FFFFFF")),

            // Semi bold

            Element<Label>().Class(StyleClassSemiBoldLabelH1)
                .Prop(Label.StylePropertyFont, resCache.GetFont("/Fonts/Imperial/Pass/SourceCodePro-SemiBold.ttf", 22))
                .Prop(Label.StylePropertyFontColor, Color.FromHex("#FFFFFF")),

            Element<Label>().Class(StyleClassSemiBoldLabelH2)
                .Prop(Label.StylePropertyFont, resCache.GetFont("/Fonts/Imperial/Pass/SourceCodePro-SemiBold.ttf", 18))
                .Prop(Label.StylePropertyFontColor, Color.FromHex("#FFFFFF")),

            Element<Label>().Class(StyleClassSemiBoldLabelH3)
                .Prop(Label.StylePropertyFont, resCache.GetFont("/Fonts/Imperial/Pass/SourceCodePro-SemiBold.ttf", 12))
                .Prop(Label.StylePropertyFontColor, Color.FromHex("#FFFFFF")),

            Element<Label>().Class(StyleClassSemiBoldLabelH4)
                .Prop(Label.StylePropertyFont, resCache.GetFont("/Fonts/Imperial/Pass/SourceCodePro-SemiBold.ttf", 10))
                .Prop(Label.StylePropertyFontColor, Color.FromHex("#FFFFFF")),

            // __________ Rich Text Labels __________

            // Bold

            Element<RichTextLabel>().Class(StyleClassBoldLabelH1)
                .Prop(Label.StylePropertyFont, resCache.GetFont("/Fonts/Imperial/Pass/SourceCodePro-Bold.ttf", 22))
                .Prop(Label.StylePropertyFontColor, Color.FromHex("#FFFFFF")),

            Element<RichTextLabel>().Class(StyleClassBoldLabelH2)
                .Prop(Label.StylePropertyFont, resCache.GetFont("/Fonts/Imperial/Pass/SourceCodePro-Bold.ttf", 18))
                .Prop(Label.StylePropertyFontColor, Color.FromHex("#FFFFFF")),

            Element<RichTextLabel>().Class(StyleClassBoldLabelH3)
                .Prop(Label.StylePropertyFont, resCache.GetFont("/Fonts/Imperial/Pass/SourceCodePro-Bold.ttf", 12))
                .Prop(Label.StylePropertyFontColor, Color.FromHex("#FFFFFF")),

            Element<RichTextLabel>().Class(StyleClassBoldLabelH4)
                .Prop(Label.StylePropertyFont, resCache.GetFont("/Fonts/Imperial/Pass/SourceCodePro-Bold.ttf", 10))
                .Prop(Label.StylePropertyFontColor, Color.FromHex("#FFFFFF")),

            // Medium

            Element<RichTextLabel>().Class(StyleClassMediumLabelH1)
                .Prop(Label.StylePropertyFont, resCache.GetFont("/Fonts/Imperial/Pass/SourceCodePro-Medium.ttf", 22))
                .Prop(Label.StylePropertyFontColor, Color.FromHex("#FFFFFF")),

            Element<RichTextLabel>().Class(StyleClassMediumLabelH2)
                .Prop(Label.StylePropertyFont, resCache.GetFont("/Fonts/Imperial/Pass/SourceCodePro-Medium.ttf", 18))
                .Prop(Label.StylePropertyFontColor, Color.FromHex("#FFFFFF")),

            Element<RichTextLabel>().Class(StyleClassMediumLabelH3)
                .Prop(Label.StylePropertyFont, resCache.GetFont("/Fonts/Imperial/Pass/SourceCodePro-Medium.ttf", 12))
                .Prop(Label.StylePropertyFontColor, Color.FromHex("#FFFFFF")),

            Element<RichTextLabel>().Class(StyleClassMediumLabelH4)
                .Prop(Label.StylePropertyFont, resCache.GetFont("/Fonts/Imperial/Pass/SourceCodePro-Medium.ttf", 10))
                .Prop(Label.StylePropertyFontColor, Color.FromHex("#FFFFFF")),

            // Semi bold

            Element<RichTextLabel>().Class(StyleClassSemiBoldLabelH1)
                .Prop(Label.StylePropertyFont, resCache.GetFont("/Fonts/Imperial/Pass/SourceCodePro-SemiBold.ttf", 22))
                .Prop(Label.StylePropertyFontColor, Color.FromHex("#FFFFFF")),

            Element<RichTextLabel>().Class(StyleClassSemiBoldLabelH2)
                .Prop(Label.StylePropertyFont, resCache.GetFont("/Fonts/Imperial/Pass/SourceCodePro-SemiBold.ttf", 18))
                .Prop(Label.StylePropertyFontColor, Color.FromHex("#FFFFFF")),

            Element<RichTextLabel>().Class(StyleClassSemiBoldLabelH3)
                .Prop(Label.StylePropertyFont, resCache.GetFont("/Fonts/Imperial/Pass/SourceCodePro-SemiBold.ttf", 12))
                .Prop(Label.StylePropertyFontColor, Color.FromHex("#FFFFFF")),

            Element<RichTextLabel>().Class(StyleClassSemiBoldLabelH4)
                .Prop(Label.StylePropertyFont, resCache.GetFont("/Fonts/Imperial/Pass/SourceCodePro-SemiBold.ttf", 10))
                .Prop(Label.StylePropertyFontColor, Color.FromHex("#FFFFFF"))

        ]);
    }
}
