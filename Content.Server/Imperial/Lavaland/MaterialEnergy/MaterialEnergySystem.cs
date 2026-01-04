using Content.Server.Power.EntitySystems;
using Content.Server.Power.Components;
using Content.Server.Stack;
using Content.Shared.Stacks;
using Content.Shared.Interaction;
using Content.Shared.Materials;
using Robust.Shared.Audio.Systems;
using Content.Shared.Power.Components;

namespace Content.Server.Imperial.Lavaland.MaterialEnergy
{
    /// <summary>
    /// It has a whitelist of materials from which you can replenish energy. 30 pieces = all the energy.
    /// </summary>
    public sealed class MaterialEnergySystem : EntitySystem
    {
        [Dependency] private readonly BatterySystem _batterySystem = default!;
        [Dependency] private readonly StackSystem _stack = default!;
        [Dependency] private readonly SharedAudioSystem _audio = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<MaterialEnergyComponent, InteractUsingEvent>(OnInteract);
        }

        private void OnInteract(EntityUid uid, MaterialEnergyComponent component, InteractUsingEvent args)
        {
            if (args.Handled || component.MaterialWhiteList == null)
                return;

            if (!TryComp<PhysicalCompositionComponent>(args.Used, out var composition))
                return;

            // Get the amount of material (for stacks and single items)
            var count = 1;
            if (TryComp<StackComponent>(args.Used, out var stack))
                count = stack.Count;

            foreach (var fuelType in component.MaterialWhiteList)
            {
                if (composition.MaterialComposition.TryGetValue(fuelType, out var materialPerItem))
                {
                    if (TryAddBatteryCharge(uid, args.Used, materialPerItem, count))
                    {
                        _audio.PlayPvs(component.ReplenishmentOfFirearm, uid);
                        args.Handled = true;
                        break;
                    }
                }
            }
        }

        private bool TryAddBatteryCharge(EntityUid batteryUid, EntityUid materialUid, int materialPerItem, int itemCount)
        {
            if (!TryComp<BatteryComponent>(batteryUid, out var battery))
                return false;

            var chargeNeeded = battery.MaxCharge - battery.CurrentCharge;
            if (chargeNeeded <= 0)
                return false;

            var totalMaterial = materialPerItem * itemCount;
            var materialToConsume = (int)Math.Min(totalMaterial, chargeNeeded);

            if (materialToConsume <= 0)
                return false;

            // Adding energy
            _batterySystem.SetCharge((batteryUid, battery), battery.CurrentCharge + materialToConsume);

            // Remove the used material
            var itemsToConsume = (int)Math.Ceiling((float)materialToConsume / materialPerItem);
            if (itemsToConsume <= 0)
                return false;

            if (TryComp<StackComponent>(materialUid, out var stack))
            {
                var toDelete = _stack.Split(materialUid, itemsToConsume, Transform(materialUid).Coordinates);
                QueueDel(toDelete);
            }
            else
            {
                QueueDel(materialUid);
            }

            return true;
        }
    }
}
