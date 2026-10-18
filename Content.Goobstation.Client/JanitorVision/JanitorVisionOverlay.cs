// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Numerics;
using Content.Goobstation.Shared.JanitorVision;
using Content.Shared.Fluids.Components;
using Content.Shared.Tag;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Enums;

namespace Content.Goobstation.Client.JanitorVision;

/// <summary>Draws scanner markers above lighting and FOV, without revealing the rest of the room.</summary>
public sealed class JanitorVisionOverlay : Overlay
{
    [Dependency] private readonly IEntityManager _entities = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IEyeManager _eyes = default!;

    private readonly SharedJanitorVisionSystem _vision;
    public override OverlaySpace Space => OverlaySpace.WorldSpace;
    public JanitorVisionComponent? Component;

    public JanitorVisionOverlay()
    {
        IoCManager.InjectDependencies(this);
        _vision = _entities.System<SharedJanitorVisionSystem>();
        ZIndex = 1;
    }

    protected override bool BeforeDraw(in OverlayDrawArgs args) => args.Viewport.Eye == _eyes.CurrentEye;

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (Component is not { Enabled: true } comp || _player.LocalEntity is not { } observer)
            return;

        // A remote camera must not project markers from the wearer's map onto another map.
        if (!_entities.TryGetComponent(observer, out TransformComponent? xform)
            || args.Viewport.Eye?.Position.MapId != xform.MapID)
            return;

        var handle = args.WorldHandle;
        handle.SetTransform(Matrix3x2.Identity);
        handle.UseShader(null);

        var trash = _entities.EntityQueryEnumerator<TagComponent, SpriteComponent>();
        while (trash.MoveNext(out var uid, out _, out var sprite))
        {
            // A puddle with tags is drawn in the puddle pass only.
            if (!_entities.HasComponent<PuddleComponent>(uid))
                DrawTarget(handle, observer, uid, sprite, comp);
        }

        var puddles = _entities.EntityQueryEnumerator<PuddleComponent, SpriteComponent>();
        while (puddles.MoveNext(out var uid, out _, out var sprite))
            DrawTarget(handle, observer, uid, sprite, comp);
    }

    private void DrawTarget(DrawingHandleWorld handle, EntityUid observer, EntityUid target,
        SpriteComponent sprite, JanitorVisionComponent comp)
    {
        if (!sprite.Visible || !_vision.TryGetTarget(observer, target, comp.Range, out var kind, out var position))
            return;

        var color = kind == JanitorVisionTarget.Puddle ? comp.PuddleColor : comp.TrashColor;
        var radius = kind == JanitorVisionTarget.Puddle ? 0.4f : 0.23f;
        handle.DrawCircle(position, radius + 0.08f, color.WithAlpha(0.15f));
        handle.DrawCircle(position, radius, color.WithAlpha(0.3f));
        handle.DrawCircle(position, radius, color, false);

        // Puddles have a round centre; trash has a cross, so colour is not the only distinction.
        if (kind == JanitorVisionTarget.Puddle)
            handle.DrawCircle(position, 0.08f, color);
        else
        {
            handle.DrawLine(position - new Vector2(0.1f, 0f), position + new Vector2(0.1f, 0f), color);
            handle.DrawLine(position - new Vector2(0f, 0.1f), position + new Vector2(0f, 0.1f), color);
        }
    }
}
