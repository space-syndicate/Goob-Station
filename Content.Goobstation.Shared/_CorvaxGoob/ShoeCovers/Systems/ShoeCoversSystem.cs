// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Numerics;
using Content.Goobstation.Common.Footprints;
using Content.Goobstation.Shared.Clothing.Components;
using Content.Goobstation.Shared.ShoeCovers.Components;
using Content.Shared.Clothing.Components;
using Content.Shared.Clothing.EntitySystems;
using Content.Shared.DoAfter;
using Content.Shared.Gravity;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory;
using Content.Shared.Whitelist;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Markdown.Mapping;

namespace Content.Goobstation.Shared.ShoeCovers.Systems;

public sealed class ShoeCoversSystem : EntitySystem
{
    private const string ShoesSlot = "shoes";
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedGravitySystem _gravity = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;

    public override void Initialize()
    {
        base.Initialize();
        // Обрабатываем снятие раньше быстрого надевания обуви. Событие помечается обработанным и на клиенте.
        SubscribeLocalEvent<WrappedShoesComponent, UseInHandEvent>(OnRemoveInHand, before: [typeof(ClothingSystem)]);
        // Сервер применяет покрытие, меняет износ и создаёт предметы при снятии бахил.
        if (!_net.IsServer)
            return;

        SubscribeLocalEvent<ShoeCoversWrapComponent, AfterInteractEvent>(OnApply);
        SubscribeLocalEvent<ShoeCoversWrapComponent, WrappedShoesDoAfterEvent>(OnApplyFinished);
        SubscribeLocalEvent<ShoeCoversWearerComponent, MoveEvent>(OnMove);
    }

    private bool CanCover(Entity<ShoeCoversWrapComponent> covers, EntityUid shoes)
    {
        // Проверяем, что бахилы ещё не израсходованы.
        return !EntityManager.IsQueuedForDeletion(covers.Owner)
            && !EntityManager.IsQueuedForDeletion(shoes)
            && !TerminatingOrDeleted(shoes)
            && !HasComp<WrappedShoesComponent>(shoes) // Ботинки ещё не обёрнуты.
            && TryComp<ClothingComponent>(shoes, out var clothing)
            // Slots — набор флагов. Проверяем, можно ли одеть на слот ног
            && (clothing.Slots & SlotFlags.FEET) != 0
            && _whitelist.CheckBoth(shoes, covers.Comp.Blacklist, covers.Comp.Whitelist);
    }

    private void OnApply(Entity<ShoeCoversWrapComponent> covers, ref AfterInteractEvent args)
    {
        var target = args.Target;
        if (args.Handled || !args.CanReach || target == null || !CanCover(covers, target.Value))
            return;

        // Если игрок сдвинется, получит достаточно урона или выпустит бахилы из рук, действие прервётся.
        args.Handled = _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager, args.User,
            covers.Comp.WrapDelay, new WrappedShoesDoAfterEvent(), covers, target: target.Value, used: covers)
        {
            NeedHand = true,
            BreakOnMove = true,
            BreakOnDamage = true,
        });
    }

    private void OnApplyFinished(Entity<ShoeCoversWrapComponent> covers, ref WrappedShoesDoAfterEvent args)
    {
        var target = args.Target;
        // Пока шло действие, ботинки могли переместить, удалить или покрыть другой парой бахил.
        if (args.Handled || args.Cancelled || target == null || !CanCover(covers, target.Value)
            || !_interaction.IsAccessible(args.User, target.Value) || !_interaction.InRangeUnobstructed(args.User, target.Value)
            || !covers.Comp.AddedComponents.TryGetComponent<WrappedShoesComponent>(Factory, out _))
            return;

        // Добавляем покрытие, сохраняя штатные компоненты обуви, в том числе выдачу эффекта галош.
        var shoes = target.Value;
        EntityManager.AddComponents(shoes, covers.Comp.AddedComponents, removeExisting: false);
        var coating = Comp<WrappedShoesComponent>(shoes);
        // Выдача маркера хранится на обуви. ClothingGrantingSystem применит её при следующем надевании.
        coating.CreatedGrantComponent = !TryComp<ClothingGrantComponentComponent>(shoes, out var grant);
        if (grant == null)
            grant = EnsureComp<ClothingGrantComponentComponent>(shoes);
        var key = Factory.GetComponentName<ShoeCoversWearerComponent>();
        // TryAdd сообщает, принадлежит ли новая запись этим бахилам. От этого зависит, можно ли удалить её позже.
        coating.AddedWearerGrant = grant.Components.TryAdd(key,
            new EntityPrototype.ComponentRegistryEntry(Factory.GetComponent<ShoeCoversWearerComponent>(), new MappingDataNode()));

        // Если обувь уже надета, событие экипировки не повторится. Выдаём маркер текущему носителю сразу.
        var parent = Transform(shoes).ParentUid;
        if (coating.AddedWearerGrant && TryComp<InventoryComponent>(parent, out var inventory)
            && _inventory.TryGetSlotEntity(parent, ShoesSlot, out var equipped, inventory) && equipped == shoes
            && !HasComp<ShoeCoversWearerComponent>(parent))
        {
            EnsureComp<ShoeCoversWearerComponent>(parent);
            // Система экипировки использует Active, чтобы при снятии обуви убрать выданный ею маркер.
            grant.Active[key] = true;
        }

        args.Handled = true;
        _audio.PlayPvs(covers.Comp.WrapSound, shoes);
        // Расходуем предмет бахил после того, как покрытие и выдача успешно добавлены.
        QueueDel(covers);
    }

    private void OnRemoveInHand(Entity<WrappedShoesComponent> shoes, ref UseInHandEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;
        if (_net.IsServer)
            // Обувь сейчас в руке. args.User задаёт место для мусора, а надетой пары здесь нет.
            RemoveCovers(shoes, args.User, wearer: null);
    }

    private void OnMove(Entity<ShoeCoversWearerComponent> wearer, ref MoveEvent args)
    {
        // Маркер находится на персонаже, а счётчик — на обуви. Получаем именно предмет из слота shoes.
        if (!_inventory.TryGetSlotEntity(wearer.Owner, ShoesSlot, out var shoes)
            || !TryComp<WrappedShoesComponent>(shoes, out var coating)
            || !args.OldPosition.IsValid(EntityManager) || !args.NewPosition.IsValid(EntityManager))
            return;

        var distance = GetDistance(args);
        // Поворот на месте тоже вызывает MoveEvent, но даёт нулевое расстояние. В невесомости бахилы не изнашиваются.
        if (distance == 0 || _gravity.IsWeightless(wearer.Owner))
            return;

        // Износ хранится на ботинках, поэтому при снятии и повторном надевании счётчик сохраняется.
        coating.Distance += distance;
        if (coating.Distance >= coating.MaxDistance)
            RemoveCovers((shoes.Value, coating), wearer.Owner, wearer.Owner);
    }

    private float GetDistance(in MoveEvent args)
    {
        // если еремещение в рамках одного грида.
        if (args.OldPosition.EntityId == args.NewPosition.EntityId)
            return Vector2.Distance(args.OldPosition.Position, args.NewPosition.Position);

        // При смене грида локальные координаты относятся к разным объектам. Сравниваем позиции в пределах одной карты.
        var oldMap = _transform.ToMapCoordinates(args.OldPosition);
        var newMap = _transform.ToMapCoordinates(args.NewPosition);
        if (oldMap.MapId == newMap.MapId && newMap.MapId != MapId.Nullspace)
            return Vector2.Distance(oldMap.Position, newMap.Position);

        return 0f;
    }

    private void RemoveCovers(Entity<WrappedShoesComponent> shoes, EntityUid dropAt, EntityUid? wearer)
    {
        // Звук и прототип мусора берём из покрытия до его удаления. Этот метод используется и при разрыве, и при снятии.
        var trash = shoes.Comp.UnwrapTrash;
        var sound = shoes.Comp.UnwrapSound;
        // Убираем только свою запись из выдачи и снимаем маркер, если его выдали именно эти ботинки.
        if (shoes.Comp.AddedWearerGrant && TryComp<ClothingGrantComponentComponent>(shoes, out var grant))
        {
            var key = Factory.GetComponentName<ShoeCoversWearerComponent>();
            grant.Components.Remove(key);
            if (grant.Active.Remove(key, out var active) && active && wearer != null)
                RemComp<ShoeCoversWearerComponent>(wearer.Value);

            // Компонент выдачи удаляем целиком, только если бахилы создали его и записей в нём больше нет.
            if (shoes.Comp.CreatedGrantComponent && grant.Components.Count == 0)
                RemComp<ClothingGrantComponentComponent>(shoes);
        }

        // Удаление компонента снимает слой на клиенте и прекращает подсчёт износа этой пары.
        RemComp<WrappedShoesComponent>(shoes);
        _audio.PlayPvs(sound, dropAt);
        if (trash != null)
            Spawn(trash.Value, Transform(dropAt).Coordinates);
    }
}
