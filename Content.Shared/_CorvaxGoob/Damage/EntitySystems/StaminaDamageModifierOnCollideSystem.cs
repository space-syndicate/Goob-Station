using Content.Shared._CorvaxGoob.Damage.Components;
using Content.Shared.Armor;
using Content.Shared.Blocking;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Projectiles;

namespace Content.Shared._CorvaxGoob.Damage.EntitySystems;

public sealed class StaminaDamageModifierOnCollideSystem : EntitySystem
{
    [Dependency] private readonly SharedStaminaSystem _stamina = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StaminaDamageModifierOnCollideComponent, ProjectileHitEvent>(OnProjectileHitEvent);
    }

    private void OnProjectileHitEvent(Entity<StaminaDamageModifierOnCollideComponent> entity, ref ProjectileHitEvent ev)
    {
        if (entity.Comp.AppliedModifier is null)
            return;

        if (!ev.Damage.DamageDict.ContainsKey(entity.Comp.AppliedModifier))
            return;

        if (!HasComp<StaminaComponent>(ev.Target))
            return;

        var armorEv = new CoefficientQueryEvent(Inventory.SlotFlags.All);
        RaiseLocalEvent(ev.Target, armorEv);

        var blunt = ev.Damage.DamageDict[entity.Comp.AppliedModifier];

        if (armorEv.DamageModifiers.Coefficients.ContainsKey(entity.Comp.AppliedModifier))
            blunt *= armorEv.DamageModifiers.Coefficients[entity.Comp.AppliedModifier];

        if (TryComp<BlockingUserComponent>(ev.Target, out var blockingUser) && TryComp<BlockingComponent>(blockingUser.BlockingItem, out var blocking))
            if (blocking.IsBlocking)
            {
                if (blocking.ActiveBlockDamageModifier.Coefficients.ContainsKey(entity.Comp.AppliedModifier))
                    blunt *= blocking.ActiveBlockDamageModifier.Coefficients[entity.Comp.AppliedModifier];
            }
            else if (blocking.PassiveBlockDamageModifer.Coefficients.ContainsKey(entity.Comp.AppliedModifier))
                blunt *= blocking.PassiveBlockDamageModifer.Coefficients[entity.Comp.AppliedModifier];

        var staminaDamage = blunt * entity.Comp.StaminaCoefficient;

        _stamina.TakeStaminaDamage(ev.Target, staminaDamage.Int());
    }
}
