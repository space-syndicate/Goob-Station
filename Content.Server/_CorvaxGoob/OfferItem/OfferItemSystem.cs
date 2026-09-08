using Content.Shared._CorvaxGoob.OfferItem;
using Content.Shared.Alert;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;

namespace Content.Server._CorvaxGoob.OfferItem;

public sealed class OfferItemSystem : SharedOfferItemSystem
{
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly AlertsSystem _alertsSystem = default!;

    private float _offerAcc = 0;
    private const float OfferAccMax = 3f;

    public override void Update(float frameTime)
    {
        _offerAcc += frameTime;

        if (_offerAcc >= OfferAccMax)
            _offerAcc -= OfferAccMax;
        else
            return;

        var query = EntityQueryEnumerator<OfferItemComponent, HandsComponent>();
        while (query.MoveNext(out var uid, out var offerItem, out var hands))
        {
            if (_hands.GetActiveHand(uid) == null)
                continue;

            if (offerItem.Hand is not null && _hands.HandIsEmpty((uid, hands), offerItem.Hand))
            {
                if (offerItem.Target is not null)
                {
                    UnReceive(offerItem.Target.Value, offerItem: offerItem);
                    offerItem.IsInOfferMode = false;
                    Dirty(uid, offerItem);
                }
                else
                    UnOffer(uid, offerItem);
            }

            if (!offerItem.IsInReceiveMode)
            {
                _alertsSystem.ClearAlert(uid, OfferAlert);
                continue;
            }

            _alertsSystem.ShowAlert(uid, OfferAlert);
        }
    }
}
