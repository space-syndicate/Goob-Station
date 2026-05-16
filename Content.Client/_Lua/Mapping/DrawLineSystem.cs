// LuaWorld - This file is licensed under AGPLv3
// Copyright (c) 2025 LuaWorld
// See AGPLv3.txt for details.
using Content.Shared._Lua.Mapping;
using Robust.Client.Graphics;

namespace Content.Client._Lua.Mapping;
public sealed class DrawLineSystem : EntitySystem
{
    [Dependency] private readonly IOverlayManager _overlay = default!;
    private DrawLineOverlay? _overlayInst;
    private bool _visible;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeNetworkEvent<DrawLineClientEvent>(OnDrawLine);
        SubscribeNetworkEvent<DrawLineRequestEvent>(_ => SendDrawRequest());
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

    public void SendDrawRequest()
    {
        RaiseNetworkEvent(new DrawLineRequestEvent());
    }
}


