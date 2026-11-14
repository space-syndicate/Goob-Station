// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Client.Overlays;
using Content.Shared._CorvaxGoob.JanitorVision;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Robust.Client.Graphics;

namespace Content.Client._CorvaxGoob.JanitorVision;

public sealed class JanitorVisionSystem : EquipmentHudSystem<JanitorVisionComponent>
{
    [Dependency] private IOverlayManager _overlays = default!;

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
        _overlay.Dispose();
        base.Shutdown();
    }

    private void OnState(Entity<JanitorVisionComponent> ent, ref AfterAutoHandleStateEvent args) => RefreshOverlay();
    private void OnChanged(Entity<JanitorVisionComponent> ent, ref JanitorVisionChangedEvent args) => RefreshOverlay();

    // These glasses only work as equipment in the eyes slot, never as an innate mob HUD.
    protected override void OnRefreshComponentHud(Entity<JanitorVisionComponent> ent,
        ref RefreshEquipmentHudEvent<JanitorVisionComponent> args) { }

    protected override void OnRefreshEquipmentHud(Entity<JanitorVisionComponent> ent,
        ref InventoryRelayedEvent<RefreshEquipmentHudEvent<JanitorVisionComponent>> args)
    {
        // ComponentRemove is raised before the inventory slot stops referring to the goggles.
        if (!ent.Comp.Deleted && ent.Comp.Enabled)
            base.OnRefreshEquipmentHud(ent, ref args);
    }

    protected override void UpdateInternal(RefreshEquipmentHudEvent<JanitorVisionComponent> args)
    {
        _overlay.Component = args.Components[0];
        if (!_overlays.HasOverlay<JanitorVisionOverlay>())
            _overlays.AddOverlay(_overlay);
    }

    protected override void DeactivateInternal()
    {
        _overlay.Component = null;
        _overlays.RemoveOverlay(_overlay);
    }
}
