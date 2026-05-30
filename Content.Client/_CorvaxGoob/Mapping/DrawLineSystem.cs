using Content.Shared._CorvaxGoob.Mapping;
using Robust.Client.Graphics;

namespace Content.Client._CorvaxGoob.Mapping;
public sealed class DrawLineSystem : EntitySystem
{
    [Dependency] private readonly IOverlayManager _overlay = default!;
    private DrawLineOverlay? _overlayInst;
    private bool _visible;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeNetworkEvent<DrawLineClientEvent>(OnDrawLine);
    }

    private void OnDrawLine(DrawLineClientEvent ev)
    {
        _visible = ev.Show;
        if (_visible)
        {
            if (_overlayInst == null) _overlayInst = new DrawLineOverlay();
            if (!_overlay.HasOverlay(typeof(DrawLineOverlay))) _overlay.AddOverlay(_overlayInst);
            _overlayInst.SetState(true, ev.Grid, ev.OriginTile, ev.TileSize);
        }
        else
        {
            if (_overlayInst != null && _overlay.HasOverlay(typeof(DrawLineOverlay)))
            {
                _overlay.RemoveOverlay(_overlayInst);
                _overlayInst.SetState(false, ev.Grid, ev.OriginTile, ev.TileSize);
            }
        }
    }
}


