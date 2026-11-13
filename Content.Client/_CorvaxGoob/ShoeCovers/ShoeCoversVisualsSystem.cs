// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Client.Clothing;
using Content.Client.Items.Systems;
using Content.Goobstation.Shared.ShoeCovers.Components;
using Content.Shared.Clothing;
using Content.Shared.Hands;
using Content.Shared.Hands.Components;
using Content.Shared.Item;
using Robust.Client.GameObjects;

namespace Content.Client._CorvaxGoob.ShoeCovers;

public sealed class ShoeCoversVisualsSystem : EntitySystem
{
    private const string LayerKey = "shoe-covers";
    [Dependency] private readonly SpriteSystem _sprite = default!;
    [Dependency] private readonly SharedItemSystem _item = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<WrappedShoesComponent, ComponentStartup>(OnStartup);
        // При запуске компонента путь к спрайту может ещё не прийти с сервера. Пробуем снова после получения состояния.
        SubscribeLocalEvent<WrappedShoesComponent, AfterAutoHandleStateEvent>(OnState);
        SubscribeLocalEvent<WrappedShoesComponent, ComponentShutdown>(OnShutdown);
        // Сначала штатные системы собирают слои обуви, затем мы дописываем поверх них слой бахил.
        SubscribeLocalEvent<WrappedShoesComponent, GetEquipmentVisualsEvent>(OnWorn, after: [typeof(ClientClothingSystem)]);
        SubscribeLocalEvent<WrappedShoesComponent, GetInhandVisualsEvent>(OnHeld, after: [typeof(ItemSystem)]);
    }

    private void OnStartup(Entity<WrappedShoesComponent> ent, ref ComponentStartup args) => Show(ent);

    private void OnState(Entity<WrappedShoesComponent> ent, ref AfterAutoHandleStateEvent args) => Show(ent);

    private void Show(Entity<WrappedShoesComponent> ent)
    {
        if (ent.Comp.OverlaySprite.Length == 0 || !TryComp<SpriteComponent>(ent, out var sprite))
            return;

        // Повторное обновление использует тот же слой и сохраняет исходный спрайт ботинок.
        // Состояние icon относится к изображению самого предмета. Для рук и ног используются другие состояния RSI.
        var layer = _sprite.LayerMapReserve((ent.Owner, sprite), LayerKey);
        _sprite.LayerSetData((ent.Owner, sprite), layer, Layer(ent.Comp, "icon"));
        // Просим перерисовать обувь, если она уже надета или находится в руке.
        _item.VisualsChanged(ent.Owner);
    }

    private void OnShutdown(Entity<WrappedShoesComponent> ent, ref ComponentShutdown args)
    {
        // При удалении всей сущности её спрайт исчезнет вместе с ней. Обновление нужно при снятии покрытия с живой обуви.
        if (TerminatingOrDeleted(ent.Owner))
            return;

        if (TryComp<SpriteComponent>(ent, out var sprite))
            _sprite.RemoveLayer((ent.Owner, sprite), LayerKey, logMissing: false);

        _item.VisualsChanged(ent.Owner);
    }

    private void OnWorn(Entity<WrappedShoesComponent> ent, ref GetEquipmentVisualsEvent args)
    {
        // Обувь в другом слоте, например на поясе, не должна рисовать бахилы на ногах.
        if (args.Slot == "shoes" && CanDraw(ent.Comp))
            args.Layers.Add((LayerKey, Layer(ent.Comp, "equipped-FEET")));
    }

    private void OnHeld(Entity<WrappedShoesComponent> ent, ref GetInhandVisualsEvent args)
    {
        if (!CanDraw(ent.Comp) || args.Location is not (HandLocation.Left or HandLocation.Right))
            return;

        // Для левой и правой руки в RSI заданы разные изображения и ключи слоя.
        var state = args.Location == HandLocation.Left ? "inhand-left" : "inhand-right";
        args.Layers.Add(($"{LayerKey}-{state}", Layer(ent.Comp, state)));
    }

    // При перерисовке из OnShutdown компонент ещё существует. Не добавляем обратно слой, который сейчас снимаем.
    private static bool CanDraw(WrappedShoesComponent comp)
        => comp.OverlaySprite.Length > 0 && comp.LifeStage < ComponentLifeStage.Stopping;

    private static PrototypeLayerData Layer(WrappedShoesComponent comp, string state)
        => new() { RsiPath = comp.OverlaySprite, State = state };
}
