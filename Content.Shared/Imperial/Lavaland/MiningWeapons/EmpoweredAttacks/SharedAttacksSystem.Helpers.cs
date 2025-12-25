using Content.Shared.DoAfter;
using Content.Shared.Wieldable.Components;

namespace Content.Shared.Imperial.Lavaland.MiningWeapons.EmpoweredAttacks;

public abstract partial class SharedAttacksSystem
{
    public bool StartDoAfter(EntityUid user, EntityUid used, float time, DoAfterEvent doAfterEvent)
    {
        var args = new DoAfterArgs(EntityManager, user, time, doAfterEvent, used)
        {
            BreakOnDamage = true,
            BreakOnMove = true
        };

        return _doAfter.TryStartDoAfter(args);
    }

    public void DoAfterCancelled(EntityUid user)
    {
        _popup.PopupEntity(Loc.GetString("empowered-attacks-doafter-closed"), user, user);
    }

    public void ItemWieldedCancelled(EntityUid user)
    {
        _popup.PopupEntity(Loc.GetString("empowered-attacks-item-wielded-false"), user, user);
    }

    public bool IsItemWielded(EntityUid item)
    {
        return TryComp<WieldableComponent>(item, out var wieldable) && wieldable.Wielded;
    }
}
