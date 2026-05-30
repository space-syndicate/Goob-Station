using Content.Shared._CorvaxGoob.Mapping;
using Content.Shared.Administration;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Map.Components;
using Content.Shared.Administration.Managers;

namespace Content.Server._CorvaxGoob.Mapping;

public sealed class DrawLineSystem : EntitySystem
{
    [Dependency] private readonly SharedTransformSystem _xform = default!;
    [Dependency] private readonly ISharedAdminManager _admins = default!;

    private readonly HashSet<ICommonSession> _active = new();

    public override void Initialize()
    {
        base.Initialize();
    }

    public void ToggleDrawForSession(ICommonSession session)
    {
        if (!_admins.HasAdminFlag(session, AdminFlags.Mapping, includeDeAdmin: true)) return;
        if (session.AttachedEntity is not { } mob || !Exists(mob)) return;
        var xform = Transform(mob);
        if (xform.MapID == MapId.Nullspace) return;
        var grid = xform.GridUid ?? xform.MapUid;
        if (grid == null) return;
        var show = !_active.Contains(session);
        if (show) _active.Add(session); else _active.Remove(session);
        ushort tileSize = 1;
        if (grid == xform.GridUid && TryComp<MapGridComponent>(grid.Value, out var mapGrid)) tileSize = mapGrid.TileSize;
        var mapPos = _xform.GetWorldPosition(xform);
        Vector2i originTile = Vector2i.Zero;
        if (grid == xform.GridUid && TryComp<MapGridComponent>(grid.Value, out var g))
        { originTile = g.WorldToTile(mapPos); }
        var net = GetNetEntity(grid.Value);
        var cev = new DrawLineClientEvent(net, originTile, tileSize, show);
        RaiseNetworkEvent(cev, Filter.SinglePlayer(session));
    }
}


