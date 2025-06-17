using System.Linq;
using Content.Shared.Imperial.HiddenSlots;
using Content.Shared.Inventory;
using Robust.Shared.Prototypes;

namespace Content.Client.Imperial.HiddenSlots;

/// <summary>
///     Client-side system responsible for determining whether
///     a specific inventory slot should be hidden/blocked
///     based on the hidden slots defined in equipped items.
/// </summary>
public sealed partial class HiddenSlotsSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;

    /// <summary>
    ///     Checks if the given slot definition is hidden for the specified entity
    ///     by looking at the HiddenSlotsComponent of equipped items in the entity's inventory.
    /// </summary>
    /// <param name="user">The entity whose inventory is checked.</param>
    /// <param name="slotDefinition">The slot definition to check.</param>
    /// <returns>True if the slot is hidden/blocked, otherwise false.</returns>
    public bool IsHidden(EntityUid user, SlotDefinition slotDefinition)
    {
        if (!TryComp<InventoryComponent>(user, out var inventory))
            return false;

        foreach (var slot in inventory.Containers)
        {
            if (slot.ContainedEntity == null)
                continue;

            if (!TryComp<HiddenSlotsComponent>(slot.ContainedEntity.Value, out var hiddenSlot))
                continue;

            if (!_proto.TryIndex(hiddenSlot.BlockedSlots, out var dataSet))
                continue;

            if (dataSet.Values.Contains(slotDefinition.Name))
                return true;
        }

        return false;
    }
}
