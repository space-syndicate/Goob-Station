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
        Entity<OfferItemComponent?> user,
        Entity<OfferItemComponent?> target,
        EntityUid used,
        string hand)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        // Re-resolve components because hands, held item, or offer state may change while the menu is open.
        if (!Resolve(user, ref user.Comp, false) ||
            !Resolve(target, ref target.Comp, false) ||
            !CanOfferItem(user, target, used, hand))
        {
            return;
        }

        user.Comp.Item = used;
        user.Comp.Hand = hand;
        user.Comp.IsInOfferMode = true;
        Dirty(user.Owner, user.Comp);

        // Reuse the existing interact-using handler so popups, alerts, and acceptance stay identical.
        var ev = new InteractUsingEvent(user.Owner, used, target.Owner, Transform(target.Owner).Coordinates);
        SetInReceiveMode(target.Owner, target.Comp, ev);
    }

    // Shared guard for showing and executing the verb. The menu can stay open while state changes,
    // so execution must pass the same checks again before starting an offer.
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
