using Content.Shared.Hands.Components;
using Content.Shared.Interaction;
using Content.Shared.Verbs;
using Robust.Shared.Utility;

namespace Content.Shared._CorvaxGoob.OfferItem;

public abstract partial class SharedOfferItemSystem
{
    private void InitializeVerbMenu()
    {
        // Пункт контекстного меню отделён от настройки кейбинда, но использует тот же поток оффера.
        SubscribeLocalEvent<OfferItemComponent, GetVerbsEvent<InteractionVerb>>(OnGetOfferItemVerb);
    }

    private void OnGetOfferItemVerb(Entity<OfferItemComponent> ent, ref GetVerbsEvent<InteractionVerb> args)
    {
        // Показываем верб только для другого валидного персонажа, пока у пользователя активный предмет.
        if (!args.CanAccess ||
            args.Hands?.ActiveHandId is not { } hand ||
            args.Using is not { } used)
        {
            return;
        }

        if (!TryComp<OfferItemComponent>(args.User, out var offerItem))
            return;

        var user = new Entity<OfferItemComponent?>(args.User, offerItem);
        var target = ent.AsNullable();
        var item = used;
        var handId = hand;

        if (!CanOfferItem(user, target, item, handId))
            return;

        // Копируем значения из ref-события до создания действия; верб выполняется позже.
        args.Verbs.Add(new InteractionVerb
        {
            Text = Loc.GetString("offer-item-verb"),
            Icon = new SpriteSpecifier.Rsi(new ResPath("/Textures/_CorvaxGoob/Misc/give_item.rsi"), "give_item_verb"),
            Act = () => TryOfferItemToTarget(user, target, item, handId),
            Priority = 1,
        });
    }

    private void TryOfferItemToTarget(
        Entity<OfferItemComponent?> user,
        Entity<OfferItemComponent?> target,
        EntityUid used,
        string hand)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        // Повторно резолвим компоненты: руки, предмет или состояние оффера могли измениться, пока меню было открыто.
        if (!Resolve(user, ref user.Comp, false) ||
            !Resolve(target, ref target.Comp, false) ||
            !CanOfferItem(user, target, used, hand))
        {
            return;
        }

        // Тот же поток, что и у кейбинда: начинаем оффер, затем связываем обе стороны.
        StartOffer(user.Owner, user.Comp, used, hand);
        LinkOffer(user.Owner, user.Comp, target.Owner, target.Comp);
    }

    // Общая проверка предпосылок для показа и выполнения верба. Меню может оставаться открытым,
    //пока состояние меняется, поэтому перед началом оффера те же проверки выполняются повторно.
    private bool CanOfferItem(
        Entity<OfferItemComponent?> user,
        Entity<OfferItemComponent?> target,
        EntityUid item,
        string hand)
    {
        if (user.Owner == target.Owner || item == target.Owner)
            return false;

        if (user.Comp is not { } offerItem ||
            target.Comp is not { } targetOfferItem ||
            !TryComp(user.Owner, out HandsComponent? userHands) ||
            !TryComp(target.Owner, out HandsComponent? targetHands) ||
            targetHands.ActiveHandId is null ||
            !_actionBlocker.CanInteract(target.Owner, null) ||
            targetOfferItem.IsInReceiveMode ||
            offerItem.Target is not null ||
            offerItem.IsInReceiveMode ||
            userHands.ActiveHandId != hand ||
            _hands.GetActiveItem(user.Owner) != item ||
            !_hands.CanDropHeld(user.Owner, hand, checkActionBlocker: false) ||
            !_actionBlocker.CanInteract(user.Owner, target.Owner))
        {
            return false;
        }

        return true;
    }
}
