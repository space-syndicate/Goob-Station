using Content.Shared.Wieldable.Components;

namespace Content.Shared.Imperial.MiningWeapons;

public sealed class MiningWeaponsHelpers : EntitySystem
{
    public bool IsItemWielded(EntityUid item)
    {
        return TryComp<WieldableComponent>(item, out var wieldable) && wieldable.Wielded;
    }
}
