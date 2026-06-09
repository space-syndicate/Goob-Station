using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;

namespace Content.Client._CorvaxGoob.Mapping;

public sealed class DrawLineSystem : EntitySystem
{
    [Dependency] private readonly IOverlayManager _overlay = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly TransformSystem _xform = default!;

    private bool _visible;

    public void ToggleDrawLine()
    {
        if (_player.LocalEntity is null)
            return;

        _visible = !_visible;

        if (!_visible && _overlay.HasOverlay<DrawLineOverlay>())
        {
            _overlay.RemoveOverlay<DrawLineOverlay>();
            return;
        }

        var xform = Transform(_player.LocalEntity.Value);

        if (xform.MapID == MapId.Nullspace)
            return;

        var grid = xform.GridUid ?? xform.MapUid;

        if (grid is null)
            return;

        ushort tileSize = 1;

        var mapPos = _xform.GetWorldPosition(xform);

        Vector2i originTile = Vector2i.Zero;
        if (grid == xform.GridUid && TryComp<MapGridComponent>(grid.Value, out var mapGrid))
        {
            tileSize = mapGrid.TileSize;
            originTile = mapGrid.WorldToTile(mapPos);
        }

        var overlay = new DrawLineOverlay();
        _overlay.AddOverlay(overlay);

        overlay.SetState(grid.Value, originTile, tileSize);
    }
}


