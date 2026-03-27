using Content.Client.UserInterface.Controls;
using Robust.Client.Graphics;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.Imperial.UI;

[Virtual]
public class TextButton : ContainerButton
{
    public Label Label { get; }
    public HLine HLinee { get; }

    private Color _linkColor = Color.FromHex("#FFF");
    private Color _normal = Color.FromHex("#FFF");
    private Color _hover = Color.FromHex("#d2a753");
    private Color _pressed = Color.FromHex("#e4cfa5");
    private Color _disabled = Color.FromHex("#313131");

    /// <summary>
    ///     How to align the text inside the button.
    /// </summary>
    [ViewVariables]
    public Label.AlignMode TextAlign { get => Label.Align; set => Label.Align = value; }

    /// <summary>
    ///     If true, the button will allow shrinking and clip text
    ///     to prevent the text from going outside the bounds of the button.
    ///     If false, the minimum size will always fit the contained text.
    /// </summary>
    [ViewVariables]
    public bool ClipText { get => Label.ClipText; set => Label.ClipText = value; }

    /// <summary>
    ///     The text displayed by the button.
    /// </summary>
    [ViewVariables]
    public string? Text { get => Label.Text; set => Label.Text = value; }

    [ViewVariables]
    public Color? OutlineColor { get => HLinee.Color; set => HLinee.Color = value; }

    [ViewVariables]
    public Color? TextColorOverride { get => Label.FontColorOverride; set => Label.FontColorOverride = value; }

    [ViewVariables]
    public Font? TextFontOverride { get => Label.FontOverride; set => Label.FontOverride = value; }

    [ViewVariables]
    public Color NormalLinkColor { get => _normal; set => _normal = value; }

    [ViewVariables]
    public Color HoverLinkColor { get => _hover; set => _hover = value; }

    [ViewVariables]
    public Color PressedLinkColor { get => _pressed; set => _pressed = value; }

    [ViewVariables]
    public Color DisabledLinkColor { get => _disabled; set => _disabled = value; }


    [ViewVariables]
    public Color LinkColor
    {
        get => _linkColor;
        set
        {
            _linkColor = value;

            if (Label != null) TextColorOverride = value;
            if (HLinee != null) OutlineColor = value;
        }
    }

    public TextButton()
    {
        IoCManager.InjectDependencies(this);

        HLinee = new HLine()
        {
            Thickness = 1,
            Color = LinkColor
        };
        Label = new Label
        {
            StyleClasses = { "LabelSubTextPassMedium" }
        };
        var box = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            Children = { Label, HLinee }
        };

        AddChild(box);
    }

    protected override void DrawModeChanged()
    {
        switch (DrawMode)
        {
            case DrawModeEnum.Normal:
                ModulateSelfOverride = _normal;
                LinkColor = _normal;

                break;
            case DrawModeEnum.Pressed:
                ModulateSelfOverride = _pressed;
                LinkColor = _pressed;

                break;
            case DrawModeEnum.Hover:
                ModulateSelfOverride = _hover;
                LinkColor = _hover;

                break;
            case DrawModeEnum.Disabled:
                ModulateSelfOverride = _disabled;
                LinkColor = _disabled;

                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}
