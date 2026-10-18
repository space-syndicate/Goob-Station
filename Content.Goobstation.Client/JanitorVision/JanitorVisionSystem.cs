// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Client.Overlays;
using Content.Goobstation.Shared.JanitorVision;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Robust.Client.Graphics;

namespace Content.Goobstation.Client.JanitorVision;

public sealed class JanitorVisionSystem : EquipmentHudSystem<JanitorVisionComponent>
{
    [Dependency] private readonly IOverlayManager _overlays = default!;

    private JanitorVisionOverlay _overlay = default!;
    protected override SlotFlags TargetSlots => SlotFlags.EYES;

    public override void Initialize()
    {
        base.Initialize();
        _overlay = new JanitorVisionOverlay();
        SubscribeLocalEvent<JanitorVisionComponent, AfterAutoHandleStateEvent>(OnState);
        SubscribeLocalEvent<JanitorVisionComponent, JanitorVisionChangedEvent>(OnChanged);
    }

    public override void Shutdown()
    {
        DeactivateInternal();
        base.Shutdown();
    }

    private void OnState(Entity<JanitorVisionComponent> ent, ref AfterAutoHandleStateEvent args) => RefreshOverlay();
    private void OnChanged(Entity<JanitorVisionComponent> ent, ref JanitorVisionChangedEvent args) => RefreshOverlay();

    // These glasses only work as equipment in the eyes slot, never as an innate mob HUD.
    protected override void OnRefreshComponentHud(Entity<JanitorVisionComponent> ent,
        ref RefreshEquipmentHudEvent<JanitorVisionComponent> args) { }

    protected override void UpdateInternal(RefreshEquipmentHudEvent<JanitorVisionComponent> args)
    {
        _overlay.Component = null;
        foreach (var comp in args.Components)
        {
            if (comp.Enabled)
            {
                _overlay.Component = comp;
                break;
            }
        }

        if (_overlay.Component == null)
            _overlays.RemoveOverlay(_overlay);
        else if (!_overlays.HasOverlay<JanitorVisionOverlay>())
            _overlays.AddOverlay(_overlay);
    }

    protected override void DeactivateInternal()
    {
        _overlay.Component = null;
        _overlays.RemoveOverlay(_overlay);
    }
}
