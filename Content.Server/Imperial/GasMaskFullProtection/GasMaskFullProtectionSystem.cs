using System.Linq;
using Content.Server.Radiation.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Inventory.Events;

namespace Content.Server.Imperial.GasMaskFullProtection;


public sealed class GasMaskFullProtectionSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GasMaskFullProtectionComponent, GotEquippedEvent>(OnEquipee);
        SubscribeLocalEvent<GasMaskFullProtectionComponent, GotUnequippedEvent>(OnUnequipee);

        SubscribeLocalEvent<GasMaskFullProtectionUserComponent, BeforeDamageChangedEvent>(OnBeforeDamageChanged);
    }

    private void OnEquipee(EntityUid uid, GasMaskFullProtectionComponent component, GotEquippedEvent args)
    {
        EnsureComp<GasMaskFullProtectionUserComponent>(args.Equipee).GasMasks.Add(uid);
    }

    private void OnUnequipee(EntityUid uid, GasMaskFullProtectionComponent component, GotUnequippedEvent args)
    {
        if (!TryComp<GasMaskFullProtectionUserComponent>(args.Equipee, out var gasMaskFullProtectionUserComponent)) return;

        gasMaskFullProtectionUserComponent.GasMasks.Remove(uid);

        if (gasMaskFullProtectionUserComponent.GasMasks.Any()) return;

        RemComp<GasMaskFullProtectionUserComponent>(args.Equipee);
    }

    private void OnBeforeDamageChanged(EntityUid uid, GasMaskFullProtectionUserComponent component, ref BeforeDamageChangedEvent args)
    {
        foreach (var mask in component.GasMasks)
        {
            if (!TryComp<GasMaskFullProtectionComponent>(mask, out var gasMaskFullProtectionComponent)) continue;
            if (!TryComp<RadiationReceiverComponent>(mask, out var radiationReceiverComponent)) continue;

            if (radiationReceiverComponent.CurrentRadiation > gasMaskFullProtectionComponent.TolerableRadiation) continue;
            if (!args.Damage.DamageDict.TryGetValue("Radiation", out var radiationDamage)) continue;

            args.Damage.DamageDict["Radiation"] = FixedPoint2.Zero;
        }
    }
}
