// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Actions;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;

namespace Content.Shared._CorvaxGoob.JanitorVision;

public sealed class SharedJanitorVisionSystem : EntitySystem
{
    [Dependency] private SharedActionsSystem _actions = default!;
    [Dependency] private InventorySystem _inventory = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<InventoryComponent, RefreshEquipmentHudEvent<JanitorVisionComponent>>(OnRefreshHud);
        SubscribeLocalEvent<JanitorVisionComponent, GetItemActionsEvent>(OnGetActions);
        SubscribeLocalEvent<JanitorVisionComponent, ToggleJanitorVisionEvent>(OnToggle);
    }

    private void OnRefreshHud(Entity<InventoryComponent> ent, ref RefreshEquipmentHudEvent<JanitorVisionComponent> args)
    {
        _inventory.RelayEvent(ent, ref args);
    }

    private void OnGetActions(Entity<JanitorVisionComponent> ent, ref GetItemActionsEvent args)
    {
        if (args.SlotFlags is not { } slots || (slots & SlotFlags.EYES) == 0)
            return;

        var previousAction = ent.Comp.ToggleActionEntity;
        args.AddAction(ref ent.Comp.ToggleActionEntity, ent.Comp.ToggleAction);
        _actions.SetToggled(ent.Comp.ToggleActionEntity, ent.Comp.Enabled);
        if (previousAction != ent.Comp.ToggleActionEntity)
            Dirty(ent);
    }

    private void OnToggle(Entity<JanitorVisionComponent> ent, ref ToggleJanitorVisionEvent args)
    {
        if (args.Handled
            || !_inventory.TryGetSlotEntity(args.Performer, "eyes", out var glasses)
            || glasses != ent.Owner)
            return;

        ent.Comp.Enabled = !ent.Comp.Enabled;
        _actions.SetToggled(ent.Comp.ToggleActionEntity, ent.Comp.Enabled);
        Dirty(ent);
        args.Handled = true;
        var changed = new JanitorVisionChangedEvent();
        RaiseLocalEvent(ent.Owner, ref changed);
    }
}
