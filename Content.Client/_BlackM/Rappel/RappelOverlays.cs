using System.Numerics;
using Content.Shared._BlackM.Rappel;
using Robust.Client.Graphics;
using Robust.Client.GameObjects;
using Robust.Client.ResourceManagement;
using Robust.Client.Player;
using Robust.Shared.Enums;
using Robust.Shared.Timing;

namespace Content.Client._BlackM.Rappel;

public sealed class RappelRopeOverlay : Overlay
{
    [Dependency] private readonly IEntityManager _entMan = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IResourceCache _resources = default!;

    [Dependency] private readonly IPlayerManager _player = default!;

    private readonly SharedTransformSystem _xform;
    private readonly RappelSystem _rappel;
    private readonly Texture _rope;
    private readonly List<Vector2> _drawnPositions = new();

    public override OverlaySpace Space => OverlaySpace.WorldSpaceEntities;

    public RappelRopeOverlay()
    {
        IoCManager.InjectDependencies(this);
        _xform = _entMan.System<SharedTransformSystem>();
        _rappel = _entMan.System<RappelSystem>();
        _rope = _resources.GetResource<TextureResource>("/Textures/_BlackM/Effects/Rappel/rope_segment.png").Texture;
        ZIndex = (int) Content.Shared.DrawDepth.DrawDepth.Objects;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        var handle = args.WorldHandle;
        var now = _timing.CurTime;
        _drawnPositions.Clear();
        var ropes = _entMan.EntityQueryEnumerator<RappelDeployedRopeComponent, TransformComponent>();
        while (ropes.MoveNext(out _, out var rope, out var ropeXform))
        {
            if (ropeXform.MapID != args.MapId ||
                rope.LifeStage > ComponentLifeStage.Running || rope.Landed || !rope.InUse)
                continue;
            var position = _xform.GetWorldPosition(ropeXform);
            if (_drawnPositions.Exists(p => Vector2.DistanceSquared(p, position) <= 0.0025f))
                continue;
            _drawnPositions.Add(position);
            var top = position + new Vector2(0f, MathF.Max(0f, rope.Height) + RappelSystem.RopeTopExtra);
            var ground = position + RappelVisualOffsets.RopeTailOffset;
            var progress = rope.DeployDuration <= 0f ? 1f : Math.Clamp(
                (float) (now - rope.DeployStart).TotalSeconds / rope.DeployDuration, 0f, 1f);
            var ease = progress * progress * (3f - 2f * progress);
            if (_rappel.TryGetRopeBody(position, args.MapId, out var harness, out var angle))
            {
                var tail = new Vector2(ground.X + (harness.X - position.X) * 0.35f, ground.Y);
                var bounds = new Box2(Vector2.Min(Vector2.Min(top, tail), harness),
                    Vector2.Max(Vector2.Max(top, tail), harness)).Enlarged(1.5f);
                if (!args.WorldAABB.Intersects(bounds))
                    continue;
                DrawCurve(handle, top, harness, -angle * 0.4f);
                var hanging = Vector2.Lerp(harness, tail, ease);
                if (Vector2.DistanceSquared(harness, hanging) > 0.0001f)
                    DrawCurve(handle, harness, hanging, MathF.Sin(ease * MathF.PI) * 0.12f - angle * 0.4f);
                continue;
            }
            var tip = Vector2.Lerp(top, ground, ease);
            if (args.WorldAABB.Intersects(Box2.FromTwoPoints(top, ground).Enlarged(1f)))
                DrawCurve(handle, top, tip, MathF.Sin(progress * MathF.PI) * 0.12f);
        }
        handle.SetTransform(Matrix3x2.Identity);
    }

    private void DrawCurve(DrawingHandleWorld handle, Vector2 from, Vector2 to, float bend)
    {
        var count = Math.Clamp((int) MathF.Ceiling(Vector2.Distance(from, to) * 4f), 1, 160);
        var prev = from;
        for (var i = 1; i <= count; i++)
        {
            var t = i / (float) count;
            var next = Vector2.Lerp(from, to, t) + new Vector2(MathF.Sin(t * MathF.PI) * bend, 0f);
            var delta = next - prev;
            var length = delta.Length();
            if (length > 0.0001f)
            {
                var rotation = MathF.Atan2(delta.Y, delta.X) - MathF.PI / 2f;
                handle.SetTransform(Matrix3Helpers.CreateTransform((prev + next) * 0.5f, rotation));
                handle.DrawTextureRect(_rope, Box2.CenteredAround(Vector2.Zero,
                    new Vector2(5f / 32f, length + 0.5f / 32f)));
            }
            prev = next;
        }
    }
}

public sealed class RappelConsoleOverlay : Overlay
{
    [Dependency] private readonly IEntityManager _entMan = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private readonly RappelSystem _rappel;

    public override OverlaySpace Space => OverlaySpace.WorldSpace;

    public RappelConsoleOverlay()
    {
        IoCManager.InjectDependencies(this);
        _rappel = _entMan.System<RappelSystem>();
        ZIndex = 6;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (_rappel.ConsoleState is not { ZoneFound: true } state || _rappel.ConsoleCamera is not { } camera)
            return;

        if (!_entMan.TryGetComponent<TransformComponent>(camera, out var camXform) || camXform.MapID != args.MapId)
            return;

        if (_rappel.ConsoleEye == null || args.Viewport.Eye != _rappel.ConsoleEye)
            return;

        var handle = args.WorldHandle;
        handle.DrawCircle(state.ZoneCenter, state.ZoneRadius, Color.White.WithAlpha(0.2f), false);
        if (state.Target is { } target)
            DrawCrosshair(handle, target, 0.18f, Color.White.WithAlpha(0.65f));
        foreach (var slot in state.AvailableSlots)
            handle.DrawCircle(slot, 0.22f, Color.FromHex("#b4e5c4"), false);
        foreach (var slot in state.ReservedSlots)
            handle.DrawCircle(slot, 0.22f, Color.FromHex("#f0c674"), false);
        foreach (var slot in state.BlockedSlots)
            DrawCrosshair(handle, slot, 0.18f, Color.FromHex("#d87575"));

        if (_rappel.ConsoleHover is { } hover &&
            Vector2.Distance(hover, state.ZoneCenter) <= state.ZoneRadius)
        {
            DrawCrosshair(handle, hover, 0.25f, Color.White.WithAlpha(0.65f));
        }
    }

    private static void DrawCrosshair(DrawingHandleWorld handle, Vector2 pos, float size, Color color)
    {
        handle.DrawCircle(pos, size, color, false);
        handle.DrawLine(pos - new Vector2(size * 1.4f, 0), pos - new Vector2(size * 0.4f, 0), color);
        handle.DrawLine(pos + new Vector2(size * 0.4f, 0), pos + new Vector2(size * 1.4f, 0), color);
        handle.DrawLine(pos - new Vector2(0, size * 1.4f), pos - new Vector2(0, size * 0.4f), color);
        handle.DrawLine(pos + new Vector2(0, size * 0.4f), pos + new Vector2(0, size * 1.4f), color);
    }
}
