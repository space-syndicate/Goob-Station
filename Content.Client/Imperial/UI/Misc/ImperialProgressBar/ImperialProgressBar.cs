using System.Numerics;
using Content.Client.Imperial.Roadmap.UI;
using Content.Shared.Imperial.ColorHelper;
using Robust.Client.Graphics;
using Robust.Shared.Graphics;
using Robust.Shared.Prototypes;

namespace Content.Client.Imperial.UI;


[Virtual]
public class ImperialProgressBar : BorderedPanelContainer
{
    [Dependency] private readonly IClyde _clyde = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    protected ShaderInstance GradientShader;
    private IRenderTexture? _renderTexture;


    [ViewVariables]
    public float Progress = 1.0f;

    [ViewVariables]
    public float GradiendIntensity = 0.3f;

    [ViewVariables]
    public float GradiendRotation = 0;

    [ViewVariables]
    public Color FirstGradientColor { get; set; } = Color.FromHex("#8B661D");

    [ViewVariables]
    public Color LastGradientColor { get; set; } = Color.FromHex("#FFBF50");

    [ViewVariables]
    public Color BackgroundProgressBarColor { get; set; } = Color.FromHex("#3D3D3D");


    public ImperialProgressBar()
    {
        IoCManager.InjectDependencies(this);

        BackgroundPanelColor = Color.White;
        GradientShader = _prototypeManager.Index<ShaderPrototype>("UIGradient").InstanceUnique();
    }


    protected override void Draw(DrawingHandleScreen handle)
    {
        BackgroundPanelColor = Color.White;

        GradientShader.SetParameter("colorLast", ColorHelper.ToVector3(LastGradientColor));
        GradientShader.SetParameter("colorFirst", ColorHelper.ToVector3(FirstGradientColor));
        GradientShader.SetParameter("colorBack", ColorHelper.ToVector3(BackgroundProgressBarColor));

        GradientShader.SetParameter("rotation", GradiendRotation * (float)(Math.PI / 180.0));
        GradientShader.SetParameter("gradientIntensity", GradiendIntensity);
        GradientShader.SetParameter("progress", Progress);

        if (_renderTexture == null || _renderTexture.Size != PixelSize)
            _renderTexture = _clyde.CreateRenderTarget(
                PixelSize,
                new RenderTargetFormatParameters(RenderTargetColorFormat.Rgba8Srgb, true),
                new TextureSampleParameters
                {
                    Filter = true
                },
                nameof(ImperialProgressBar)
            );

        handle.RenderInRenderTarget(_renderTexture, () => base.Draw(handle), Color.Transparent);

        handle.UseShader(GradientShader);

        handle.DrawTexture(_renderTexture.Texture, Vector2.Zero);

        handle.UseShader(null);
    }
}
