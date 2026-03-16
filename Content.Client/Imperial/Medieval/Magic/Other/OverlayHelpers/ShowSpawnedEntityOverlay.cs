using System.Linq;
using System.Numerics;
using Content.Shared.Imperial.ColorHelper;
using Content.Shared.Imperial.Medieval.Magic;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Shared.Enums;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Client.Imperial.OverlayTargetingHelpers.PrespawnOverlayHelper;



public sealed class ShowSpawnedEntityOverlay : Overlay
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IInputManager _inputManager = default!;
    [Dependency] private readonly IEntitySystemManager _entitySystem = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly IEyeManager _eyeManager = default!;
    private readonly SpriteSystem _spriteSystem;
    private readonly SharedTransformSystem _transformSystem;

    public override OverlaySpace Space => OverlaySpace.WorldSpace;


    private readonly ShaderInstance _shader;
    private readonly string _shaderId = "PrespawnOverlay";

    public List<PrespawnedEntitySprite> Sprites = new();
    public Angle Rotation = Angle.FromDegrees(0);

    public readonly Vector2 StandardResolution = new Vector2(672, 480);


    public ShowSpawnedEntityOverlay()
    {
        IoCManager.InjectDependencies(this);

        _shader = _prototypeManager.Index<ShaderPrototype>(_shaderId).InstanceUnique();

        _spriteSystem = _entitySystem.GetEntitySystem<SpriteSystem>();
        _transformSystem = _entitySystem.GetEntitySystem<SharedTransformSystem>();
    }

    protected override bool BeforeDraw(in OverlayDrawArgs args) => Sprites.Any();

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (_eyeManager.MainViewport.Window == null) return;

        var handle = args.WorldHandle;

        var mousePosition = _eyeManager.PixelToMap(_inputManager.MouseScreenPosition);
        var playerPosition = _eyeManager.PixelToMap(new ScreenCoordinates((Vector2)_eyeManager.MainViewport.Window.Size / 2, _eyeManager.MainViewport.Window.Id));

        var distance = Vector2.Distance(playerPosition.Position, mousePosition.Position);

        if (mousePosition.MapId != args.MapId) return;
        if (!_mapManager.TryFindGridAt(mousePosition, out var gridUid, out var _)) return;

        var worldMatrix = _transformSystem.GetWorldMatrix(gridUid);
        var invMatrix = _transformSystem.GetInvWorldMatrix(gridUid);

        handle.SetTransform(worldMatrix);

        var localPos = Vector2.Transform(mousePosition.Position, invMatrix);

        foreach (var sprite in Sprites)
        {
            if (!sprite.RequiredAngle.EqualsApprox(Rotation) && !sprite.AlwaysRender) continue;

            DrawSprite(handle, localPos, distance, sprite);
        }

        handle.SetTransform(Matrix3x2.Identity);
        handle.UseShader(null);
    }

    #region Helpers

    private void DrawSprite(DrawingHandleWorld handle, Vector2 localPos, float distance, PrespawnedEntitySprite sprite)
    {
        var inRange = distance <= sprite.Range;

        var spriteTexture = _spriteSystem.Frame0(sprite.DrawedSprite);
        var color = inRange ? sprite.InRangeColor : sprite.OutOfRangeColor;

        var aabb = Box2.UnitCentered.Translated(localPos);
        var box = new Box2Rotated(aabb, Rotation, localPos);

        _shader.SetParameter("mix_color", sprite.MixColor);
        _shader.SetParameter("opacity", sprite.Opacity);
        _shader.SetParameter("color", ColorHelper.ToVector3(color ?? Color.Transparent));

        handle.UseShader(_shader);

        handle.DrawTextureRect(spriteTexture, box, sprite.SpriteColor);

        handle.UseShader(null);
    }

    #endregion
}
