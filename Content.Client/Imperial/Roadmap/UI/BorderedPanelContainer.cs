

using System.Numerics;
using Robust.Client.Graphics;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Prototypes;

namespace Content.Client.Imperial.Roadmap.UI;


[Virtual]
public class BorderedPanelContainer : PanelContainer
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    private static string _borderRadiusShaderProtoId = "BorderRadius";


    private float _borderRadius = 0f;
    private Color _backgroundColor = Color.FromHex("#25252A");
    private Vector2 _artifactScaleProtection = new Vector2(1.0f);


    protected ShaderInstance BorderShader;


    [ViewVariables(VVAccess.ReadWrite)]
    public Color BackgroundPanelColor
    {
        get => _backgroundColor;
        set => _backgroundColor = value;
    }

    [ViewVariables(VVAccess.ReadWrite)]
    public float BorderRadius
    {
        get => _borderRadius * 100;
        set
        {
            _borderRadius = value / 100;
            BorderShader.SetParameter("borderRadius", value / 100);
        }
    }

    [ViewVariables(VVAccess.ReadWrite)]
    public Vector2 ArtifactScaleProtection
    {
        get => _artifactScaleProtection;
        set
        {
            _artifactScaleProtection = value;
            BorderShader.SetParameter("artifactScaleProtection", value);
        }
    }

    public BorderedPanelContainer()
    {
        IoCManager.InjectDependencies(this);

        BorderShader = _prototypeManager.Index<ShaderPrototype>(_borderRadiusShaderProtoId).InstanceUnique();

        BorderShader.SetParameter("artifactScaleProtection", _artifactScaleProtection);
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        BorderShader.SetParameter("elementSize", PixelSize);

        handle.UseShader(BorderShader);

        handle.DrawRect(PixelSizeBox, BackgroundPanelColor);

        handle.UseShader(null);
    }
}
