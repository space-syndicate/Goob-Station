using Content.Client.Items;
using Content.Client.Weapons.Ranged.Components;
using Content.Client.Weapons.Ranged.Systems;
using Content.Shared.Imperial.DeimonFly.RussianRoulette;

namespace Content.Client.Imperial.DeimonFly.RussianRoulette;

/// <summary>
/// Скрывает индикатор камер барабана у револьверов для русской рулетки.
/// </summary>
public sealed class RussianRouletteGunStatusSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        // Стандартная система оружия сначала добавляет индикатор, после чего мы удаляем только его.
        SubscribeLocalEvent<RussianRouletteGunComponent, ItemStatusCollectMessage>(
            OnCollectItemStatus,
            after: [typeof(GunSystem)]);
    }

    private void OnCollectItemStatus(Entity<RussianRouletteGunComponent> gun, ref ItemStatusCollectMessage args)
    {
        if (!TryComp<AmmoCounterComponent>(gun, out var ammoCounter) || ammoCounter.Control == null)
            return;

        args.Controls.Remove(ammoCounter.Control);
    }
}
