using System.Numerics;
using Content.Shared._Aquila.Heretic;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Client._Aquila.Heretic;

public sealed class MansusHandArmOverlay : Overlay
{
    private static readonly ResPath ArmRsi = new("/Textures/_Aquila/Heretic/mansus_hand.rsi");
    private const float TrailLength = 3.5f;

    [Dependency] private readonly IEntityManager _entity = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private readonly SpriteSystem _sprite;
    private readonly SharedTransformSystem _transform;
    private readonly Dictionary<string, SpriteSpecifier.Rsi> _armSprites = new();

    public override OverlaySpace Space => OverlaySpace.WorldSpaceBelowEntities;

    public MansusHandArmOverlay()
    {
        IoCManager.InjectDependencies(this);
        _sprite = _entity.System<SpriteSystem>();
        _transform = _entity.System<SharedTransformSystem>();
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        var handle = args.WorldHandle;
        var query = _entity.EntityQueryEnumerator<MansusHandComponent, TransformComponent>();
        while (query.MoveNext(out _, out var hand, out var xform))
        {
            if (xform.MapID != args.MapId)
                continue;

            var end = _transform.GetWorldPosition(xform);
            var delta = end - hand.Origin;
            var length = MathF.Min(delta.Length(), TrailLength);
            if (length < 0.5f)
                continue;

            var direction = delta / delta.Length();
            var rotation = direction.ToWorldAngle();
            if (!_armSprites.TryGetValue(hand.ArmState, out var armSprite))
            {
                armSprite = new SpriteSpecifier.Rsi(ArmRsi, hand.ArmState);
                _armSprites[hand.ArmState] = armSprite;
            }

            var texture = _sprite.GetFrame(armSprite, _timing.CurTime);

            for (var step = 0.5f; step < length; step += 1f)
            {
                var center = end - direction * step;
                var box = Box2.CenteredAround(center, Vector2.One);
                var alpha = 1f - step / TrailLength;
                handle.DrawTextureRect(texture, new Box2Rotated(box, rotation, center), Color.White.WithAlpha(alpha));
            }
        }
    }
}
