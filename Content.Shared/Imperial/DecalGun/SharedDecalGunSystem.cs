using Content.Shared.Containers.ItemSlots;

namespace Content.Shared.Imperial.DecalGun;

/// <summary>
/// Provides shared logic for decal gun systems, focusing on interaction with the gun’s magazine.
/// Includes helper methods for retrieving the currently inserted magazine component.
/// Abstract base to be extended by both client and server decal gun systems.
/// </summary>
public abstract class SharedDecalGunSystem : EntitySystem
{
    [Dependency] private readonly ItemSlotsSystem _slots = default!;

    /// <summary>
    /// Retrieves the current decal magazine component from the specified entity's "magazine" slot.
    /// Returns null if the slot is missing, empty, or contains an invalid component.
    /// </summary>
    protected Entity<DecalGunMagComponent>? GetCurrentMag(EntityUid uid)
    {
        if (!_slots.TryGetSlot(uid, "magazine", out var slot) ||
            slot.Item == null)
            return null;

        if (!TryComp<DecalGunMagComponent>(slot.Item.Value, out var magComp))
            return null;

        return (slot.Item.Value, magComp);
    }
}


