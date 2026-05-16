// LuaWorld - This file is licensed under AGPLv3
// Copyright (c) 2025 LuaWorld
// See AGPLv3.txt for details.
using Robust.Shared.Serialization;

namespace Content.Shared._Lua.Mapping;

[Serializable, NetSerializable]
public sealed class DrawLineRequestEvent : EntityEventArgs
{
}

[Serializable, NetSerializable]
public sealed class DrawLineClientEvent : EntityEventArgs
{
    public NetEntity Grid;
    public Vector2i OriginTile;
    public ushort TileSize;
    public bool Show;

    public DrawLineClientEvent(NetEntity grid, Vector2i originTile, ushort tileSize, bool show)
    {
        Grid = grid;
        OriginTile = originTile;
        TileSize = tileSize;
        Show = show;
    }
}


