// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Numerics;
using Content.Shared.Actions;
using Content.Shared.Fluids.Components;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.Tag;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Goobstation.Shared.JanitorVision;

public sealed class SharedJanitorVisionSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SharedContainerSystem _containers = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly TagSystem _tags = default!;

    private static readonly ProtoId<TagPrototype> TrashTag = "Trash";

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

        args.AddAction(ref ent.Comp.ToggleActionEntity, ent.Comp.ToggleAction);
        _actions.SetToggled(ent.Comp.ToggleActionEntity, ent.Comp.Enabled);
        Dirty(ent);
    }

    private void OnToggle(Entity<JanitorVisionComponent> ent, ref ToggleJanitorVisionEvent args)
    {
        if (args.Handled || !_inventory.TryGetSlotEntity(args.Performer, "eyes", out var glasses) || glasses != ent.Owner)
            return;

        ent.Comp.Enabled = !ent.Comp.Enabled;
        _actions.SetToggled(ent.Comp.ToggleActionEntity, ent.Comp.Enabled);
        Dirty(ent);
        args.Handled = true;
        var changed = new JanitorVisionChangedEvent();
        RaiseLocalEvent(ent.Owner, ref changed);
    }

    /// <summary>
    /// Selects only loose trash and puddles on the observer's map and within range.
    /// Deliberately has no line-of-sight check: walls must not block the scanner.
    /// Collected trash, held items, and entities outside the client's PVS are excluded.
    /// </summary>
    public bool TryGetTarget(EntityUid observer, EntityUid target, float range,
        out JanitorVisionTarget kind, out Vector2 position)
    {
        kind = default;
        position = default;
        if (range <= 0f || Deleted(observer) || Deleted(target)
            || !TryComp(observer, out TransformComponent? observerXform)
            || !TryComp(target, out TransformComponent? targetXform)
            || observerXform.MapID == MapId.Nullspace || observerXform.MapID != targetXform.MapID
            || (MetaData(target).Flags & MetaDataFlags.Detached) != 0
            || _containers.IsEntityInContainer(target))
            return false;

        if (HasComp<PuddleComponent>(target))
            kind = JanitorVisionTarget.Puddle;
        else if (!_tags.HasTag(target, TrashTag))
            return false;

        position = _transform.GetWorldPosition(targetXform);
        return Vector2.DistanceSquared(_transform.GetWorldPosition(observerXform), position) <= range * range;
    }
}
