using System.Linq;
using System.Numerics;
using Content.Shared.Imperial.ColorHelper;
using Content.Shared.Imperial.Medieval.Magic.Minigames;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.Graphics;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Client.Imperial.Medieval.Magic.Minigames;


public sealed class MedievalArrowsMinigameOverlay : Overlay
{
    [Dependency] private readonly IEntitySystemManager _entitySystem = default!;
    [Dependency] private readonly IEyeManager _eyeManager = default!;
    [Dependency] private readonly IConfigurationManager _configurationManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IClyde _clyde = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    private readonly SpriteSystem _spriteSystem;

    private static string _arrowMinigameShader = "ArrowMinigame";


    public override OverlaySpace Space => OverlaySpace.ScreenSpace;



    private readonly ShaderInstance _shader = default!;
    private readonly Dictionary<Vector2, IRenderTexture> _buffers = new();



    public List<ArrowsTypes> Combination = new();
    public List<(TimeSpan, bool)> PlayerCombination = new();
    public Color ValidColor = Color.FromHex("#00A600");
    public Color InvalidColor = Color.FromHex("#A60000");
    public float RightPadding = 0.05f;
    public float LeftPadding = 0.05f;
    public float BottomPadding = 0.15f;
    public float MaxAnimationStep = 10.0f;
    public float ReversedAnimationPoint = 0.5f;


    public readonly string ArrowUpPath = "/Textures/Imperial/Medieval/Interface/Magic/up.png";
    public readonly string ArrowUpFilledPath = "/Textures/Imperial/Medieval/Interface/Magic/up_filled.png";

    public readonly string ArrowLeftPath = "/Textures/Imperial/Medieval/Interface/Magic/left.png";
    public readonly string ArrowLeftFilledPath = "/Textures/Imperial/Medieval/Interface/Magic/left_filled.png";

    public readonly string ArrowDownPath = "/Textures/Imperial/Medieval/Interface/Magic/down.png";
    public readonly string ArrowDownFilledPath = "/Textures/Imperial/Medieval/Interface/Magic/down_filled.png";

    public readonly string ArrowRightPath = "/Textures/Imperial/Medieval/Interface/Magic/right.png";
    public readonly string ArrowRightFilledPath = "/Textures/Imperial/Medieval/Interface/Magic/right_filled.png";


    public MedievalArrowsMinigameOverlay()
    {
        IoCManager.InjectDependencies(this);

        _shader = _prototypeManager.Index<ShaderPrototype>(_arrowMinigameShader).InstanceUnique();
        _spriteSystem = _entitySystem.GetEntitySystem<SpriteSystem>();
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (_eyeManager.MainViewport.Window == null) return;

        var screen = args.ScreenHandle;
        var scale = _configurationManager.GetCVar(CVars.DisplayUIScale);
        scale = scale == 0 ? 1 : scale;

        var leftPadding = _eyeManager.MainViewport.Window.Size.X * (LeftPadding * scale);
        var rightPadding = _eyeManager.MainViewport.Window.Size.X * (RightPadding * scale);
        var horizontalSize = _eyeManager.MainViewport.Window.Size.X - rightPadding - leftPadding;

        var scaleMatrix = Matrix3Helpers.CreateScale(new Vector2(scale));
        var matrix = Matrix3x2.Multiply(Matrix3x2.Identity, scaleMatrix);

        screen.SetTransform(matrix);

        CleanTrash();

        for (var i = 0; i < Combination.Count; i++)
        {
            var invertedI = Combination.Count - 1 - i;
            var arrow = Combination[invertedI];

            var arrowAnimationProgress = GetArrowAnimatedPosition(invertedI);
            var animationPosition = PlayerCombination.TryGetValue(invertedI, out var playerComb)
                ? ComputeArrowAnimationDirection(arrowAnimationProgress, arrow, playerComb.Item2)
                : ComputeArrowAnimationDirection(arrowAnimationProgress, arrow, false);

            var texture = GetArrowTexture(arrow);
            var textureSize = texture.Size * scale;

            var currentArrowsSize = textureSize.X * i;
            var sizeInOneHorizontalLine = MathF.Floor(horizontalSize / textureSize.X) * textureSize.X;

            var y = MathF.Floor(currentArrowsSize / horizontalSize);

            if (!_buffers.TryGetValue(textureSize, out var _))
                _buffers.Add(
                    textureSize,
                    _clyde.CreateRenderTarget(
                        (Vector2i)textureSize * 2,
                        new RenderTargetFormatParameters(RenderTargetColorFormat.Rgba8Srgb, true),
                        new TextureSampleParameters
                        {
                            Filter = true
                        }, nameof(MedievalArrowsMinigameOverlay)
                    )
                );

            var buffer = _buffers[textureSize];

            screen.RenderInRenderTarget(buffer, () =>
            {
                if (PlayerCombination.TryGetValue(invertedI, out var playerArrow))
                    _shader.SetParameter("color", playerArrow.Item2 ? ColorHelper.ToVector3(InvalidColor) : ColorHelper.ToVector3(ValidColor));

                screen.UseShader(_shader);

                if (PlayerCombination.TryGetValue(invertedI, out var _)) screen.DrawTexture(GetFilledArrowTexture(arrow), Vector2.Zero);

                screen.UseShader(null);

                screen.DrawTexture(texture, Vector2.Zero);
            }, Color.Transparent);

            var halfTextureSize = textureSize / 2;

            var arrowsInOneHorizontalLine = sizeInOneHorizontalLine / textureSize.X;
            var totalArrowsInHorizontalLine = (y + 1) * arrowsInOneHorizontalLine;
            var arrowsInLine = totalArrowsInHorizontalLine < Combination.Count ? arrowsInOneHorizontalLine : arrowsInOneHorizontalLine - (totalArrowsInHorizontalLine - Combination.Count);
            var currentLineSize = arrowsInLine * textureSize.X;

            var lineAlignment = (horizontalSize - currentLineSize) / 2;

            var position = new Vector2(
                horizontalSize - textureSize.X * i - textureSize.X + sizeInOneHorizontalLine * y + leftPadding - lineAlignment,
                _eyeManager.MainViewport.Window!.Size.Y - _eyeManager.MainViewport.Window!.Size.Y * BottomPadding * scale * (y + 1)
            ) + animationPosition;

            screen.DrawTexture(buffer.Texture, position / scale);
        }

        screen.SetTransform(Matrix3x2.Identity);
        screen.UseShader(null);
    }

    #region Helpers

    private void CleanTrash()
    {
        var clone = PlayerCombination.ToList();

        for (var i = 0; i < clone.Count; i++)
        {
            var arrow = clone[i];
            var progress = GetArrowAnimatedPosition(i);

            if (progress != 0.0f) continue;
            if (!arrow.Item2) continue;

            PlayerCombination.Remove(arrow);
        }
    }

    private float GetArrowAnimatedPosition(int i)
    {
        if (!PlayerCombination.TryGetValue(i, out var playerCombination)) return 0.0f;

        var progress = (float)(_timing.CurTime - playerCombination.Item1).TotalSeconds * 100;

        if (MaxAnimationStep < progress)
            return 0.0f;

        if (MaxAnimationStep * ReversedAnimationPoint < progress)
            progress = MaxAnimationStep - progress;

        return progress;
    }

    private Vector2 ComputeArrowAnimationDirection(float progress, ArrowsTypes arrow, bool isInversed)
    {
        if (isInversed)
            return arrow switch
            {
                ArrowsTypes.ArrowUp => new Vector2(0.0f, progress),
                ArrowsTypes.ArrowDown => new Vector2(0.0f, -progress),
                ArrowsTypes.ArrowLeft => new Vector2(progress, 0.0f),
                ArrowsTypes.ArrowRight => new Vector2(-progress, 0.0f),
                _ => new Vector2(0.0f, 0.0f)
            };

        return arrow switch
        {
            ArrowsTypes.ArrowUp => new Vector2(0.0f, -progress),
            ArrowsTypes.ArrowDown => new Vector2(0.0f, progress),
            ArrowsTypes.ArrowLeft => new Vector2(-progress, 0.0f),
            ArrowsTypes.ArrowRight => new Vector2(progress, 0.0f),
            _ => new Vector2(0.0f, 0.0f)
        };
    }

    private Texture GetArrowTexture(ArrowsTypes arrow)
    {
        return arrow switch
        {
            ArrowsTypes.ArrowUp => _spriteSystem.Frame0(new SpriteSpecifier.Texture(new(ArrowUpPath))),
            ArrowsTypes.ArrowLeft => _spriteSystem.Frame0(new SpriteSpecifier.Texture(new(ArrowLeftPath))),
            ArrowsTypes.ArrowRight => _spriteSystem.Frame0(new SpriteSpecifier.Texture(new(ArrowRightPath))),
            ArrowsTypes.ArrowDown => _spriteSystem.Frame0(new SpriteSpecifier.Texture(new(ArrowDownPath))),
            _ => _spriteSystem.Frame0(new SpriteSpecifier.Texture(new(ArrowUpPath)))
        };
    }

    private Texture GetFilledArrowTexture(ArrowsTypes arrow)
    {
        return arrow switch
        {
            ArrowsTypes.ArrowUp => _spriteSystem.Frame0(new SpriteSpecifier.Texture(new(ArrowUpFilledPath))),
            ArrowsTypes.ArrowLeft => _spriteSystem.Frame0(new SpriteSpecifier.Texture(new(ArrowLeftFilledPath))),
            ArrowsTypes.ArrowRight => _spriteSystem.Frame0(new SpriteSpecifier.Texture(new(ArrowRightFilledPath))),
            ArrowsTypes.ArrowDown => _spriteSystem.Frame0(new SpriteSpecifier.Texture(new(ArrowDownFilledPath))),
            _ => _spriteSystem.Frame0(new SpriteSpecifier.Texture(new(ArrowUpFilledPath)))
        };
    }

    #endregion

    #region Public API

    public bool TryAddCorrectCombination()
    {
        if (Combination.Count < PlayerCombination.Count + 1) return false;

        PlayerCombination.Add((_timing.CurTime, false));

        return true;
    }

    public bool TryRemoveLastPlayerCombination()
    {
        if (PlayerCombination.Count - 1 < 0) return false;

        var toRemoveIndex = PlayerCombination.Count - 1;
        for (toRemoveIndex = PlayerCombination.Count - 1; toRemoveIndex >= 0; toRemoveIndex--)
        {
            if (!PlayerCombination[toRemoveIndex].Item2) break;
        }

        PlayerCombination[toRemoveIndex] = (_timing.CurTime, true);

        return true;
    }

    #endregion
}
