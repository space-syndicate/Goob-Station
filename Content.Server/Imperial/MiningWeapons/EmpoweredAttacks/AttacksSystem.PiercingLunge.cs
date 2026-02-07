using Content.Shared.Damage.Systems;
using Content.Shared.Imperial.MiningWeapons.EmpoweredAttacks.PiercingLunge;
using Robust.Shared.Physics.Events;

namespace Content.Server.Imperial.MiningWeapons.EmpoweredAttacks;

public sealed partial class AttacksSystem
{
    [Dependency] private readonly DamageableSystem _damageable = default!;

    private void InitializePiercingLunge()
    {
        SubscribeLocalEvent<UserPiercingLungeComponent, StartCollideEvent>(OnStartCollide);
    }

    private void OnStartCollide(EntityUid user, UserPiercingLungeComponent userComp, StartCollideEvent args)
    {
        if (!userComp.CanDamage)
            return;

        if (userComp.Damage != null)
            _damageable.TryChangeDamage(args.OtherEntity, userComp.Damage, interruptsDoAfters: false);
    }
}
