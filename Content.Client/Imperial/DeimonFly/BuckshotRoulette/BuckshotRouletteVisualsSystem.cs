using Content.Client.Clothing;
using Content.Client.Items.Systems;
using Content.Client.Weapons.Ranged.Components;
using Content.Shared.Clothing;
using Content.Shared.Hands;
using Content.Shared.Imperial.DeimonFly.BuckshotRoulette;
using Content.Shared.Inventory;
using Content.Shared.Item;
using Robust.Client.GameObjects;

namespace Content.Client.Imperial.DeimonFly.BuckshotRoulette;

/// <summary>
/// Переключает внешний вид дробовика дилера на клиенте, не вмешиваясь в штатные системы оружия и рук.
/// </summary>
public sealed class BuckshotRouletteVisualsSystem : EntitySystem
{
    [Dependency] private readonly SharedItemSystem _item = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BuckshotRouletteShotgunComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<BuckshotRouletteShotgunComponent, AfterAutoHandleStateEvent>(OnAfterState);
        SubscribeLocalEvent<BuckshotRouletteShotgunComponent, GetInhandVisualsEvent>(OnGetInhandVisuals,
            after: [typeof(ItemSystem)]);
        SubscribeLocalEvent<BuckshotRouletteShotgunComponent, GetEquipmentVisualsEvent>(OnGetEquipmentVisuals,
            after: [typeof(ClientClothingSystem)]);
    }

    private void OnStartup(Entity<BuckshotRouletteShotgunComponent> shotgun, ref ComponentStartup args)
    {
        ApplyWorldVisuals(shotgun, force: true);
    }

    private void OnAfterState(Entity<BuckshotRouletteShotgunComponent> shotgun, ref AfterAutoHandleStateEvent args)
    {
        ApplyWorldVisuals(shotgun);
    }

    private void ApplyWorldVisuals(Entity<BuckshotRouletteShotgunComponent> shotgun, bool force = false)
    {
        if (!force &&
            shotgun.Comp.AppliedBarrelVisualState == shotgun.Comp.BarrelVisualState)
        {
            return;
        }

        if (!TryComp<SpriteComponent>(shotgun, out var sprite) ||
            !_sprite.LayerMapTryGet((shotgun.Owner, sprite), GunVisualLayers.Base, out var layer, false))
        {
            return;
        }

        var (rsi, state) = shotgun.Comp.BarrelVisualState switch
        {
            BuckshotRouletteBarrelVisualState.Sawed => (shotgun.Comp.SawedWorldSprite, "icon"),
            BuckshotRouletteBarrelVisualState.Restoring => (shotgun.Comp.RestoringWorldSprite, "restoring-icon"),
            _ => (shotgun.Comp.IntactWorldSprite, "icon"),
        };

        _sprite.LayerSetRsi((shotgun.Owner, sprite), layer, rsi, state);
        shotgun.Comp.AppliedBarrelVisualState = shotgun.Comp.BarrelVisualState;

        // Если предмет сейчас находится в руках или слоте экипировки, просим владельца сразу перерисовать его.
        _item.VisualsChanged(shotgun);
    }

    private void OnGetInhandVisuals(
        Entity<BuckshotRouletteShotgunComponent> shotgun,
        ref GetInhandVisualsEvent args)
    {
        if (shotgun.Comp.BarrelVisualState == BuckshotRouletteBarrelVisualState.Intact)
            return;

        // ItemSystem уже выбрала правильное состояние с учётом HeldPrefix и наличия его в RSI.
        // Меняем только RSI штатного базового слоя, не удаляя дополнительные визуальные слои предмета.
        var regularState = $"inhand-{args.Location.ToString().ToLowerInvariant()}";
        var wieldedState = $"wielded-{regularState}";
        foreach (var (_, layer) in args.Layers)
        {
            if (layer.State != regularState && layer.State != wieldedState)
                continue;

            layer.RsiPath = shotgun.Comp.SawedInhandSprite.ToString();
            return;
        }
    }

    private void OnGetEquipmentVisuals(
        Entity<BuckshotRouletteShotgunComponent> shotgun,
        ref GetEquipmentVisualsEvent args)
    {
        if (shotgun.Comp.BarrelVisualState == BuckshotRouletteBarrelVisualState.Intact)
            return;

        if (!_inventory.TryGetSlot(args.Equipee, args.Slot, out var slot))
            return;

        var state = slot.SlotFlags switch
        {
            SlotFlags.BACK => "equipped-BACKPACK",
            SlotFlags.SUITSTORAGE => "equipped-SUITSTORAGE",
            _ => null,
        };

        if (state == null)
            return;

        // ClothingSystem уже сформировала базовый слой с правильным ключом, масштабом и состоянием.
        foreach (var (_, layer) in args.Layers)
        {
            if (layer.State != state)
                continue;

            layer.RsiPath = shotgun.Comp.SawedWorldSprite.ToString();
            return;
        }
    }
}
