using JetBrains.Annotations;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.Enums;
using Robust.Shared.Map.Components;
using System.Numerics;

namespace Content.Client._CorvaxGoob.Mapping;

[UsedImplicitly]
public sealed class DrawLineOverlay : Overlay
{
    private readonly IEntityManager _ent;

    public override OverlaySpace Space => OverlaySpace.WorldSpace | OverlaySpace.ScreenSpace;

    private EntityUid _grid;
    private Vector2i _originTile;
    private ushort _tileSize;
    private readonly IEyeManager _eye;
    private readonly Font _font;

    public DrawLineOverlay()
    {
        _ent = IoCManager.Resolve<IEntityManager>();
        _eye = IoCManager.Resolve<IEyeManager>();
        var cache = IoCManager.Resolve<IResourceCache>();
        _font = new VectorFont(cache.GetResource<FontResource>("/Fonts/NotoSans/NotoSans-Regular.ttf"), 10);
    }

    public void SetState(EntityUid grid, Vector2i origin, ushort tileSize)
    {
        _grid = grid;
        _originTile = origin;
        _tileSize = tileSize;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        switch (args.Space)
        {
            case OverlaySpace.WorldSpace: DrawWorld(args); break;
            case OverlaySpace.ScreenSpace: DrawScreen(args); break;
        }
    }

    private void DrawWorld(in OverlayDrawArgs args)
    {
        if (!_ent.TryGetComponent<MapGridComponent>(_grid, out var grid)) return;
        var xform = _ent.GetComponent<TransformComponent>(_grid);
        var xformSys = _ent.System<SharedTransformSystem>();
        var (_, _, worldMatrix, invWorld) = xformSys.GetWorldPositionRotationMatrixWithInv(xform);
        var handle = args.WorldHandle;
        handle.SetTransform(worldMatrix);
        var max = 1000;
        var microHalf = 4;
        var smallHalf = 10;
        var mediumHalf = 15;
        var color = Color.LimeGreen.WithAlpha(0.8f);
        var gridLocalVisible = invWorld.TransformBox(args.WorldBounds);
        void DrawTile(Vector2i tile)
        {
            var centre = (tile + Vector2Helpers.Half) * _tileSize;
            if (!gridLocalVisible.Contains(centre)) return;
            handle.DrawRect(Box2.CenteredAround(centre, new Vector2(_tileSize, _tileSize)), color, false);
        }
        DrawTile(_originTile);
        for (var dx = 1; dx <= max; dx++)
        {
            DrawTile(_originTile + new Vector2i(dx, 0));
            DrawTile(_originTile + new Vector2i(-dx, 0));
        }
        for (var dy = 1; dy <= max; dy++)
        {
            DrawTile(_originTile + new Vector2i(0, dy));
            DrawTile(_originTile + new Vector2i(0, -dy));
        }
        void DrawZoneSquare(int halfTiles)
        {
            if (halfTiles > max) return;
            var centreLocal = (_originTile + Vector2Helpers.Half) * _tileSize;
            var sizeLocal = new Vector2((halfTiles * 2 + 1) * _tileSize, (halfTiles * 2 + 1) * _tileSize);
            var bounds = Box2.CenteredAround(centreLocal, sizeLocal).Enlarged(_tileSize);
            if (!gridLocalVisible.Intersects(bounds)) return;
            handle.DrawRect(Box2.CenteredAround(centreLocal, sizeLocal), color, false);
        }

        DrawZoneSquare(microHalf);
        DrawZoneSquare(smallHalf);
        DrawZoneSquare(mediumHalf);
        handle.SetTransform(Matrix3x2.Identity);
    }

    private void DrawScreen(in OverlayDrawArgs args)
    {
        var handle = args.ScreenHandle;

        var xform = _ent.GetComponent<TransformComponent>(_grid);
        var xformSys = _ent.System<SharedTransformSystem>();
        var (_, _, matrix, invMatrix) = xformSys.GetWorldPositionRotationMatrixWithInv(xform);
        var numbersMax = 1000;
        var gridLocalVisible = invMatrix.TransformBox(args.WorldBounds);
        void DrawNumAt(Vector2i tile, int val)
        {
            var localCentre = (tile + Vector2Helpers.Half) * _tileSize;
            if (!gridLocalVisible.Contains(localCentre)) return;
            var worldCenter = Vector2.Transform(localCentre, matrix);
            var screenCenter = _eye.WorldToScreen(worldCenter) + new Vector2(-6, -6);
            handle.DrawString(_font, screenCenter, val.ToString());
        }
        DrawNumAt(_originTile, 1);
        for (var i = 1; i < numbersMax; i++)
        {
            var val = i + 1;
            DrawNumAt(_originTile + new Vector2i(i, 0), val);
            DrawNumAt(_originTile + new Vector2i(-i, 0), val);
            DrawNumAt(_originTile + new Vector2i(0, i), val);
            DrawNumAt(_originTile + new Vector2i(0, -i), val);
        }
    }
}
