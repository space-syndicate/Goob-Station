using Robust.Shared.Serialization;

namespace Content.Shared._CorvaxGoob.Mapping;

[Serializable, NetSerializable]
public sealed class DrawLineClientEvent(NetEntity grid, Vector2i originTile, ushort tileSize, bool show) : EntityEventArgs
{
    public NetEntity Grid = grid;
    public Vector2i OriginTile = originTile;
    public ushort TileSize = tileSize;
    public bool Show = show;
}


