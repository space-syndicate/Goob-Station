// LuaWorld - This file is licensed under AGPLv3
// Copyright (c) 2025 LuaWorld
// See AGPLv3.txt for details.
using JetBrains.Annotations;
using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.Map.Components;
using System.Numerics;
using Robust.Client.ResourceManagement;

namespace Content.Client._Lua.Mapping;

[UsedImplicitly]
public sealed class DrawLineOverlay : Overlay
{
    private readonly IEntityManager _ent;

    public override OverlaySpace Space => OverlaySpace.WorldSpace | OverlaySpace.ScreenSpace;

    private bool _show;
    private NetEntity _grid;
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

    public void SetState(bool show, NetEntity grid, Vector2i origin, ushort tileSize)
    {
        _show = show;
        _grid = grid;
        _originTile = origin;
        _tileSize = tileSize;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (!_show) return;
        switch (args.Space)
        {
            case OverlaySpace.WorldSpace: DrawWorld(args); break;
            case OverlaySpace.ScreenSpace: DrawScreen(args); break;
        }
    }

    private void DrawWorld(in OverlayDrawArgs args)
    {
        if (!_ent.TryGetEntity(_grid, out var gridEnt) || gridEnt == null) return;
        var gridUid = gridEnt.Value;
        if (!_ent.TryGetComponent<MapGridComponent>(gridUid, out var grid)) return;
        var xform = _ent.GetComponent<TransformComponent>(gridUid);
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
        if (!_ent.TryGetEntity(_grid, out var gridEnt) || gridEnt == null) return;
        var gridUid = gridEnt.Value;
        if (!_ent.TryGetComponent<MapGridComponent>(gridUid, out var grid)) return;
        var xform = _ent.GetComponent<TransformComponent>(gridUid);
        var xformSys = _ent.System<SharedTransformSystem>();
        var (_, _, matrix, invMatrix) = xformSys.GetWorldPositionRotationMatrixWithInv(xform);
        var numbersMax = 1000;
        const int microHalf = 4;
        const int smallHalf = 10;
        const int mediumHalf = 15;
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
// Commented by CorvaxGoob
    /*
        void DrawLabelTiles(int tiles, string text)
        {
            var originLocal = (_originTile + Vector2Helpers.Half) * _tileSize;
            var northLocal = originLocal + new Vector2(0, -tiles * _tileSize);
            var southLocal = originLocal + new Vector2(0, tiles * _tileSize);
            var westLocal = originLocal + new Vector2(-tiles * _tileSize, 0);
            var eastLocal = originLocal + new Vector2(tiles * _tileSize, 0);
            void DrawLabel(Vector2 local)
            {
                var world = Vector2.Transform(local, matrix);
                var screen = _eye.WorldToScreen(world);
                handle.DrawString(_font, screen + new Vector2(-18, -10), text);
            }
            DrawLabel(northLocal);
            DrawLabel(southLocal);
            DrawLabel(westLocal);
            DrawLabel(eastLocal);
        }
        const int labelMicro = 4;
        const int labelSmall = 10;
        const int labelMedium = 15;
        const int labelLarge = 20;
        DrawLabelTiles(labelMicro, "\nMicro");
        DrawLabelTiles(labelSmall, "\nSmall");
        DrawLabelTiles(labelMedium, "\nMedium");
        DrawLabelTiles(labelLarge, "\nLarge");
        */
    }
} 