using System.Linq;
using Content.Server.Actions;
using Content.Server.Administration.Logs;
using Content.Server.PDA.Ringer;
using Content.Server.Stack;
using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.Database;
using Content.Shared.FixedPoint;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Imperial.ImperialStore;
using Content.Shared.Mind;
using Content.Shared.PDA.Ringer;
using Content.Shared.UserInterface;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server.Imperial.ImperialStore;

public sealed partial class ImperialStoreSystem
{
    [Dependency] private readonly IAdminLogManager _admin = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly ActionsSystem _actions = default!;
    [Dependency] private readonly ActionContainerSystem _actionContainer = default!;
    [Dependency] private readonly ActionUpgradeSystem _actionUpgrade = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly StackSystem _stack = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    private void InitializeUi()
    {
        SubscribeLocalEvent<ImperialStoreComponent, ImperialStoreRequestUpdateInterfaceMessage>(OnRequestUpdate);
        SubscribeLocalEvent<ImperialStoreComponent, ImperialStoreBuyListingMessage>(OnBuyRequest);
        SubscribeLocalEvent<ImperialStoreComponent, ImperialStoreRequestWithdrawMessage>(OnRequestWithdraw);
        SubscribeLocalEvent<ImperialStoreComponent, ImperialStoreRequestRefundMessage>(OnRequestRefund);
        SubscribeLocalEvent<ImperialStoreComponent, ImperialRefundEntityDeletedEvent>(OnRefundEntityDeleted);
    }

    private void OnRefundEntityDeleted(Entity<ImperialStoreComponent> ent, ref ImperialRefundEntityDeletedEvent args)
    {
        ent.Comp.BoughtEntities.Remove(args.Uid);
    }

    /// <summary>
    /// Toggles the store Ui open and closed
    /// </summary>
    /// <param name="user">the person doing the toggling</param>
    /// <param name="storeEnt">the store being toggled</param>
    /// <param name="component"></param>
    public void ToggleUi(EntityUid user, EntityUid storeEnt, ImperialStoreComponent? component = null)
    {
        if (!Resolve(storeEnt, ref component))
            return;

        if (!TryComp<ActorComponent>(user, out var actor))
            return;

        if (!_ui.TryToggleUi(storeEnt, ImperialStoreUiKey.Key, actor.PlayerSession))
            return;

        UpdateUserInterface(user, storeEnt, component);
    }

    /// <summary>
    /// Closes the store UI for everyone, if it's open
    /// </summary>
    public void CloseUi(EntityUid uid, ImperialStoreComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        _ui.CloseUi(uid, ImperialStoreUiKey.Key);
    }

    /// <summary>
    /// Updates the user interface for a store and refreshes the listings
    /// </summary>
    /// <param name="user">The person who if opening the store ui. Listings are filtered based on this.</param>
    /// <param name="store">The store entity itself</param>
    /// <param name="component">The store component being refreshed.</param>
    public void UpdateUserInterface(EntityUid? user, EntityUid store, ImperialStoreComponent? component = null)
    {
        if (!Resolve(store, ref component))
            return;

        //this is the person who will be passed into logic for all listing filtering.
        if (user != null) //if we have no "buyer" for this update, then don't update the listings
        {
            component.LastAvailableListings = GetAvailableListings(component.AccountOwner ?? user.Value, store, component).ToHashSet();
        }

        //dictionary for all currencies, including 0 values for currencies on the whitelist
        Dictionary<ProtoId<ImperialCurrencyPrototype>, FixedPoint2> allCurrency = new();
        foreach (var supported in component.CurrencyWhitelist)
        {
            allCurrency.Add(supported, FixedPoint2.Zero);

            if (component.Balance.TryGetValue(supported, out var value))
                allCurrency[supported] = value;
        }

        // TODO: if multiple users are supposed to be able to interact with a single BUI & see different
        // stores/listings, this needs to use session specific BUI states.

        // only tell operatives to lock their uplink if it can be locked
        var showFooter = HasComp<RingerUplinkComponent>(store);
        var state = new ImperialStoreUpdateState(component.LastAvailableListings, allCurrency, showFooter, component.RefundAllowed);
        _ui.SetUiState(store, ImperialStoreUiKey.Key, state);
    }

    private void OnRequestUpdate(EntityUid uid, ImperialStoreComponent component, ImperialStoreRequestUpdateInterfaceMessage args)
    {
        UpdateUserInterface(args.Actor, GetEntity(args.Entity), component);
    }

    private void BeforeActivatableUiOpen(EntityUid uid, ImperialStoreComponent component, BeforeActivatableUIOpenEvent args)
    {
        UpdateUserInterface(args.User, uid, component);
    }

    /// <summary>
    /// Handles whenever a purchase was made.
    /// </summary>
    private void OnBuyRequest(EntityUid uid, ImperialStoreComponent component, ImperialStoreBuyListingMessage msg)
    {
        var listing = component.Listings.FirstOrDefault(x => x.Equals(msg.Listing));

        if (listing == null) //make sure this listing actually exists
        {
            Log.Debug("listing does not exist");
            return;
        }

        var buyer = msg.Actor;

        //verify that we can actually buy this listing and it wasn't added
        if (!ListingHasCategory(listing, component.Categories))
            return;

        //condition checking because why not
        if (listing.Conditions != null)
        {
            var args = new ImperialListingConditionArgs(component.AccountOwner ?? buyer, uid, listing, EntityManager);
            var conditionsMet = listing.Conditions.All(condition => condition.Condition(args));

            if (!conditionsMet)
                return;
        }

        //check that we have enough money
        foreach (var currency in listing.Cost)
        {
            if (!component.Balance.TryGetValue(currency.Key, out var balance) || balance < currency.Value)
            {
                return;
            }
        }

        if (!IsOnStartingMap(uid, component))
            component.RefundAllowed = false;

        //subtract the cash
        foreach (var (currency, value) in listing.Cost)
        {
            component.Balance[currency] -= value;

            component.BalanceSpent.TryAdd(currency, FixedPoint2.Zero);

            component.BalanceSpent[currency] += value;
        }

        //spawn entity
        if (listing.ProductEntity != null)
        {
            var product = Spawn(listing.ProductEntity, Transform(buyer).Coordinates);
            _hands.PickupOrDrop(buyer, product);

            HandleRefundComp(uid, component, product);

            var xForm = Transform(product);

            if (xForm.ChildCount > 0)
            {
                var childEnumerator = xForm.ChildEnumerator;
                while (childEnumerator.MoveNext(out var child))
                {
                    component.BoughtEntities.Add(child);
                }
            }
        }

        //give action
        if (!string.IsNullOrWhiteSpace(listing.ProductAction))
        {
            EntityUid? actionId;
            // I guess we just allow duplicate actions?
            // Allow duplicate actions and just have a single list buy for the buy-once ones.
            if (!_mind.TryGetMind(buyer, out var mind, out _))
                actionId = _actions.AddAction(buyer, listing.ProductAction);
            else
                actionId = _actionContainer.AddAction(mind, listing.ProductAction);

            // Add the newly bought action entity to the list of bought entities
            // And then add that action entity to the relevant product upgrade listing, if applicable
            if (actionId != null)
            {
                HandleRefundComp(uid, component, actionId.Value);

                foreach (var upgradeId in listing.ProductUpgradeId)
                {
                    foreach (var upgradeListing in component.Listings)
                    {
                        if (upgradeListing.ID != upgradeId) continue;

                        upgradeListing.ProductActionEntity = actionId.Value;
                        break;
                    }
                }
            }
        }

        if (listing is { ProductUpgradeId: not null, ProductActionEntity: not null })
        {
            if (!TryComp<ActionUpgradeComponent>(listing.ProductActionEntity, out var actionUpgradeComponent))
            {
                if (listing.ProductActionEntity != null) HandleRefundComp(uid, component, listing.ProductActionEntity.Value);

                return;
            }

            if (listing.ProductActionEntity != null)
                component.BoughtEntities.Remove(listing.ProductActionEntity.Value);

            if (
                !_actionUpgrade.TryUpgradeAction(
                    listing.ProductActionEntity,
                    out var upgradeActionId,
                    actionUpgradeComponent,
                    actionUpgradeComponent.Level + listing.ActionLevelUp
                )
            )
            {
                if (listing.ProductActionEntity != null) HandleRefundComp(uid, component, listing.ProductActionEntity.Value);

                return;
            }

            listing.ProductActionEntity = upgradeActionId;

            if (upgradeActionId != null)
                HandleRefundComp(uid, component, upgradeActionId.Value);

            foreach (var upgradeListing in component.Listings)
            {
                foreach (var upgradeId in listing.ProductUpgradeId)
                {
                    if (upgradeListing.ID != upgradeId) continue;

                    upgradeListing.ProductActionEntity = listing.ProductActionEntity;
                    break;
                }
            }
        }

        if (listing.ProductEvent != null)
        {
            if (!listing.RaiseProductEventOnUser)
                RaiseLocalEvent(listing.ProductEvent);
            else
                RaiseLocalEvent(buyer, listing.ProductEvent);
        }

        //log dat shit.
        _admin.Add(LogType.StorePurchase,
            LogImpact.Low,
            $"{ToPrettyString(buyer):player} purchased listing \"{ImperialListingLocalisationHelpers.GetLocalisedNameOrEntityName(listing, _prototypeManager)}\" from {ToPrettyString(uid)}");

        listing.PurchaseAmount++; //track how many times something has been purchased
        _audio.PlayEntity(component.BuySuccessSound, msg.Actor, uid); //cha-ching!

        UpdateUserInterface(buyer, uid, component);
    }

    /// <summary>
    /// Handles dispensing the currency you requested to be withdrawn.
    /// </summary>
    /// <remarks>
    /// This would need to be done should a currency with decimal values need to use it.
    /// not quite sure how to handle that
    /// </remarks>
    private void OnRequestWithdraw(EntityUid uid, ImperialStoreComponent component, ImperialStoreRequestWithdrawMessage msg)
    {
        //make sure we have enough cash in the bank and we actually support this currency
        if (!component.Balance.TryGetValue(msg.Currency, out var currentAmount) || currentAmount < msg.Amount)
            return;

        //make sure a malicious client didn't send us random shit
        if (!_proto.TryIndex<ImperialCurrencyPrototype>(msg.Currency, out var proto))
            return;

        //we need an actually valid entity to spawn. This check has been done earlier, but just in case.
        if (proto.Cash == null || !proto.CanWithdraw)
            return;

        var buyer = msg.Actor;

        FixedPoint2 amountRemaining = msg.Amount;
        var coordinates = Transform(buyer).Coordinates;

        var sortedCashValues = proto.Cash.Keys.OrderByDescending(x => x).ToList();
        foreach (var value in sortedCashValues)
        {
            var cashId = proto.Cash[value];
            var amountToSpawn = (int)MathF.Floor((float)(amountRemaining / value));
            var ents = _stack.SpawnMultiple(cashId, amountToSpawn, coordinates);

            _hands.PickupOrDrop(buyer, ents.First());
            amountRemaining -= value * amountToSpawn;
        }

        component.Balance[msg.Currency] -= msg.Amount;
        UpdateUserInterface(buyer, uid, component);
    }

    private void OnRequestRefund(EntityUid uid, ImperialStoreComponent component, ImperialStoreRequestRefundMessage args)
    {
        // TODO: Remove guardian/holopara

        if (args.Actor is not { Valid: true } buyer)
            return;

        if (!IsOnStartingMap(uid, component))
        {
            component.RefundAllowed = false;
            UpdateUserInterface(buyer, uid, component);
        }

        if (!component.RefundAllowed || component.BoughtEntities.Count == 0)
            return;

        _admin.Add(LogType.StoreRefund, LogImpact.Low, $"{ToPrettyString(buyer):player} has refunded their purchases from {ToPrettyString(uid):store}");

        for (var i = component.BoughtEntities.Count - 1; i >= 0; i--)
        {
            var purchase = component.BoughtEntities[i];

            if (!Exists(purchase))
                continue;

            component.BoughtEntities.RemoveAt(i);

            var action = _actions.GetAction(purchase);
            if (action != null)
                _actionContainer.RemoveAction((action.Value.Owner, action.Value.Comp));

            EntityManager.DeleteEntity(purchase);
        }

        component.BoughtEntities.Clear();

        foreach (var (currency, value) in component.BalanceSpent)
        {
            component.Balance[currency] += value;
        }
        // Reset store back to its original state
        RefreshAllListings(component);
        component.BalanceSpent = new();
        UpdateUserInterface(buyer, uid, component);
    }

    private void HandleRefundComp(EntityUid uid, ImperialStoreComponent component, EntityUid purchase)
    {
        component.BoughtEntities.Add(purchase);
        var refundComp = EnsureComp<ImperialStoreRefundComponent>(purchase);
        refundComp.StoreEntity = uid;
    }

    private bool IsOnStartingMap(EntityUid store, ImperialStoreComponent component)
    {
        var xform = Transform(store);
        return component.StartingMap == xform.MapUid;
    }

    /// <summary>
    ///     Disables refunds for this store
    /// </summary>
    public void DisableRefund(EntityUid store, ImperialStoreComponent? component = null)
    {
        if (!Resolve(store, ref component))
            return;

        component.RefundAllowed = false;
    }
}
