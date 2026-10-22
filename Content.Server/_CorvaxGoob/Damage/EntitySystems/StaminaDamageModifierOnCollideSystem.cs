using Content.Shared._CorvaxGoob.Damage.Components;
using Content.Shared.Armor;
using Content.Shared.Blocking;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Projectiles;

namespace Content.Server._CorvaxGoob.Damage.EntitySystems;

public sealed class StaminaDamageModifierOnCollideSystem : EntitySystem
{
    [Dependency] private readonly SharedStaminaSystem _stamina = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StaminaDamageModifierOnCollideComponent, AfterProjectileHitEvent>(OnProjectileHitEvent);
    }

    private void OnProjectileHitEvent(Entity<StaminaDamageModifierOnCollideComponent> entity, ref AfterProjectileHitEvent ev)
    {
        if (entity.Comp.AppliedModifier is null || ev.ModifiedDamage is null)
            return;

        if (!ev.ModifiedDamage.DamageDict.ContainsKey(entity.Comp.AppliedModifier))
            return;

        if (!HasComp<StaminaComponent>(ev.Target))
            return;

        var armorEv = new CoefficientStaminaQueryEvent(Shared.Inventory.SlotFlags.All);
        RaiseLocalEvent(ev.Target, armorEv);

        var blunt = ev.ModifiedDamage.DamageDict[entity.Comp.AppliedModifier];

        var staminaDamage = blunt * entity.Comp.StaminaCoefficient * armorEv.StaminaDamage;

        _stamina.TakeStaminaDamage(ev.Target, staminaDamage.Float());
    }
}
