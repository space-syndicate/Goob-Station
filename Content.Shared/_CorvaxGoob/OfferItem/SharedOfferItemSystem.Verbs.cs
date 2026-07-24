// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Hands.Components;
using Content.Shared.Interaction;
using Content.Shared.Verbs;
using Robust.Shared.Utility;

namespace Content.Shared._CorvaxGoob.OfferItem;

public abstract partial class SharedOfferItemSystem
{
    private void InitializeVerbMenu()
    {
        // Keep the context-menu entry separate from the keybind setup while sharing the same offer flow.
        SubscribeLocalEvent<OfferItemComponent, GetVerbsEvent<InteractionVerb>>(OnGetOfferItemVerb);
    }

    private void OnGetOfferItemVerb(Entity<OfferItemComponent> ent, ref GetVerbsEvent<InteractionVerb> args)
    {
        // Show the verb only for another valid character while the user is holding an active item.
        if (!args.CanAccess ||
            args.Hands?.ActiveHandId is not { } hand ||
            args.Using is not { } used ||
            !CanOfferItem(args.User, ent.Owner, used, hand))
        {
            return;
        }

        var user = args.User;
        var target = ent.Owner;
        var item = used;
        var handId = hand;

        // Copy values out of the ref event before creating the action; verb execution happens later.
        args.Verbs.Add(new InteractionVerb
        {
            Text = Loc.GetString("offer-item-verb"),
            Icon = new SpriteSpecifier.Rsi(new ResPath("/Textures/_CorvaxGoob/Misc/give_item.rsi"), "give_item_verb"),
            Act = () => TryOfferItemToTarget(user, target, item, handId),
            Priority = 1,
        });
    }

    private void TryOfferItemToTarget(
        EntityUid user,
        EntityUid target,
        EntityUid used,
        string hand)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        // Revalidate because hands, held item, or offer state may change while the menu is open.
        if (!CanOfferItem(user, target, used, hand) ||
            !TryComp<OfferItemComponent>(target, out var targetOfferItem) ||
            !TryComp<OfferItemComponent>(user, out var offerItem))
        {
            return;
        }

        offerItem.Item = used;
        offerItem.Hand = hand;
        offerItem.IsInOfferMode = true;
        Dirty(user, offerItem);

        // Reuse the existing interact-using handler so popups, alerts, and acceptance stay identical.
        var ev = new InteractUsingEvent(user, used, target, Transform(target).Coordinates);
        SetInReceiveMode(target, targetOfferItem, ev);
    }

    // Shared guard for showing and executing the verb. The menu can stay open while state changes,
    // so execution must pass the same checks again before starting an offer.
    private bool CanOfferItem(EntityUid user, EntityUid target, EntityUid item, string hand)
    {
        if (user == target || item == target)
            return false;

        if (!TryComp<OfferItemComponent>(target, out var targetOfferItem) ||
            !TryComp<OfferItemComponent>(user, out var offerItem) ||
            !TryComp<HandsComponent>(user, out var hands) ||
            !TryComp<HandsComponent>(target, out var targetHands) ||
            targetHands.ActiveHandId is null ||
            targetOfferItem.IsInReceiveMode ||
            offerItem.Target is not null ||
            offerItem.IsInReceiveMode ||
            hands.ActiveHandId != hand ||
            _hands.GetActiveItem(user) != item ||
            !_actionBlocker.CanInteract(user, target))
        {
            return false;
        }

        return true;
    }
}
