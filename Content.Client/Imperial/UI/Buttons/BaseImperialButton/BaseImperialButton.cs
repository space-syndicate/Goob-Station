using Content.Shared.Imperial.ColorHelper;
using Robust.Client.Graphics;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Prototypes;

namespace Content.Client.Imperial.UI;


[Virtual]
public class BaseImperialButton : Button
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    protected ShaderInstance GradientShader;

    [ViewVariables]
    public float GradiendIntensity = 0.3f;
    [ViewVariables]
    public float GradiendRotation = 0;


    [ViewVariables]
    public Color NormalColor { set { FirstGradientColorNormal = value; LastGradientColorNormal = value; } }
    [ViewVariables]
    public Color HoverColor { set { FirstGradientColorHover = value; LastGradientColorHover = value; } }
    [ViewVariables]
    public Color PressedColor { set { FirstGradientColorPressed = value; LastGradientColorPressed = value; } }
    [ViewVariables]
    public Color DisabledColor { set { FirstGradientColorDisabled = value; LastGradientColorDisabled = value; } }


    [ViewVariables]
    public Color FirstGradientColorNormal { get; set; }
    [ViewVariables]
    public Color LastGradientColorNormal { get; set; }

    [ViewVariables]
    public Color FirstGradientColorHover { get; set; }
    [ViewVariables]
    public Color LastGradientColorHover { get; set; }

    [ViewVariables]
    public Color FirstGradientColorPressed { get; set; }
    [ViewVariables]
    public Color LastGradientColorPressed { get; set; }

    [ViewVariables]
    public Color FirstGradientColorDisabled { get; set; }
    [ViewVariables]
    public Color LastGradientColorDisabled { get; set; }


    public BaseImperialButton()
    {
        IoCManager.InjectDependencies(this);

        GradientShader = _prototypeManager.Index<ShaderPrototype>("UIGradient").InstanceUnique();

        FirstGradientColorNormal = Color.FromHex("#d1992c");
        LastGradientColorNormal = Color.FromHex("#d1992c");

        FirstGradientColorHover = Color.FromHex("#d2a753");
        LastGradientColorHover = Color.FromHex("#d2a753");

        FirstGradientColorPressed = Color.FromHex("#e4cfa5");
        LastGradientColorPressed = Color.FromHex("#e4cfa5");

        FirstGradientColorDisabled = Color.FromHex("#313131");
        LastGradientColorDisabled = Color.FromHex("#313131");
    }

    protected override void DrawModeChanged()
    {
        // Nothing. We change draw mode with shaders
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        var (firstColor, lastColor) = GetCurrentDrawModeColors();

        GradientShader.SetParameter("colorFirst", ColorHelper.ToVector3(firstColor));
        GradientShader.SetParameter("colorLast", ColorHelper.ToVector3(lastColor));

        GradientShader.SetParameter("rotation", GradiendRotation * (float)(Math.PI / 180.0));
        GradientShader.SetParameter("gradientIntensity", GradiendIntensity);
        GradientShader.SetParameter("progress", 1.0f);

        handle.UseShader(GradientShader);

        base.Draw(handle);

        handle.UseShader(null);
    }

    #region Helpers

    private (Color FirstColor, Color SecondColor) GetCurrentDrawModeColors()
    {
        return DrawMode switch
        {
            DrawModeEnum.Normal => (FirstGradientColorNormal, LastGradientColorNormal),
            DrawModeEnum.Pressed => (FirstGradientColorPressed, LastGradientColorPressed),
            DrawModeEnum.Hover => (FirstGradientColorHover, LastGradientColorHover),
            DrawModeEnum.Disabled => (FirstGradientColorDisabled, LastGradientColorDisabled),
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    #endregion
}
