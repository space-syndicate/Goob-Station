using System.Numerics;
using Content.Shared._BlackM.Rappel;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Enums;
using Robust.Shared.Graphics;
using Direction = Robust.Shared.Maths.Direction;
using Robust.Shared.Timing;

namespace Content.Client._BlackM.Rappel;

public sealed class RappelSelfOverlay : Overlay
{
    [Dependency] private readonly IEntityManager _entities = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IClyde _clyde = default!;

    private readonly SharedTransformSystem _transform;
    private readonly SpriteSystem _sprites;
    private readonly IRenderTexture _bodyTarget;

    public override OverlaySpace Space => OverlaySpace.ScreenSpace;

    public RappelSelfOverlay()
    {
        IoCManager.InjectDependencies(this);
        _transform = _entities.System<SharedTransformSystem>();
        _sprites = _entities.System<SpriteSystem>();
        _bodyTarget = _clyde.CreateRenderTarget((128, 128),
            new RenderTargetFormatParameters(RenderTargetColorFormat.Rgba8Srgb, true),
            new TextureSampleParameters { Filter = false }, nameof(RappelSelfOverlay));
        ZIndex = 10;
    }

    protected override void DisposeBehavior()
    {
        _bodyTarget.Dispose();
        base.DisposeBehavior();
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (args.ViewportControl is not { } viewport ||
            _player.LocalEntity is not { } uid ||
            !_entities.TryGetComponent<RappellingComponent>(uid, out var rappel) ||
            rappel.LifeStage > ComponentLifeStage.Running ||
            !_entities.TryGetComponent<TransformComponent>(uid, out var xform) ||
            xform.MapID != args.MapId ||
            !_entities.TryGetComponent<EyeComponent>(uid, out var eye) ||
            eye.Eye == null || args.Viewport.Eye != eye.Eye ||
            !_entities.System<RappelSystem>().TryGetBody(uid, out var proxy, out var sprite))
            return;

        var now = _timing.CurTime;
        if (rappel.Preparing || SharedRappelSystem.GetProgress(rappel, now) >= 1f)
            return;
        var world = _transform.GetWorldPosition(xform);
        var origin = rappel.OffsetSaved ? rappel.OriginalOffset : Vector2.Zero;
        var offset = _entities.System<RappelSystem>().TryGetSwing(uid, out var swing)
            ? swing
            : SharedRappelSystem.GetDescentOffset(rappel, now);
        var body = world + origin + offset;
        var screen = args.ScreenHandle;
        var pixelsPerMeter = Vector2.Distance(viewport.WorldToScreen(world),
            viewport.WorldToScreen(world + Vector2.UnitY));
        if (pixelsPerMeter <= 0f)
            return;

        var matrix = viewport.GetWorldToScreenMatrix();

        screen.RenderInRenderTarget(_bodyTarget, () =>
        {
            var oldOffset = sprite.Offset;
            try
            {
                _sprites.SetOffset((proxy, sprite), Vector2.Zero);
                screen.DrawEntity(proxy, new Vector2(64f), Vector2.One,
                    Angle.Zero, Angle.Zero, Direction.South, sprite);
            }
            finally
            {
                _sprites.SetOffset((proxy, sprite), oldOffset);
            }
        }, Color.Transparent);

        screen.SetTransform(Matrix3x2.Identity);
        var scale = pixelsPerMeter / 32f;
        var size = new Vector2(128f * scale);
        var center = Vector2.Transform(body, matrix);
        screen.DrawTextureRect(_bodyTarget.Texture, UIBox2.FromDimensions(center - size / 2f, size));
    }
}
