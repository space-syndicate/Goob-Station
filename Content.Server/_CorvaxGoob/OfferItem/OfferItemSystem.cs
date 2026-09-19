using Content.Shared._CorvaxGoob.OfferItem;
using Content.Shared.Alert;

namespace Content.Server._CorvaxGoob.OfferItem;

public sealed class OfferItemSystem : SharedOfferItemSystem
{
    [Dependency] private readonly AlertsSystem _alertsSystem = default!;

    private float _offerAcc;
    private const float OfferAccMax = 3f;

    public override void Update(float frameTime)
    {
        _offerAcc += frameTime;

        if (_offerAcc >= OfferAccMax)
            _offerAcc -= OfferAccMax;
        else
            return;

        var query = EntityQueryEnumerator<OfferItemComponent>();
        while (query.MoveNext(out var uid, out var offerItem))
        {
            if (offerItem.IsInReceiveMode)
                _alertsSystem.ShowAlert(uid, OfferAlert);
            else
                _alertsSystem.ClearAlert(uid, OfferAlert);
        }
    }

    // Внутренние обёртки над переходами состояния для интеграционных тестов.
    internal void StartOffer(EntityUid uid, OfferItemComponent comp, EntityUid item, string hand)
        => base.StartOffer(uid, comp, item, hand);

    internal void LinkOffer(EntityUid user, OfferItemComponent userComp, EntityUid target, OfferItemComponent targetComp)
        => base.LinkOffer(user, userComp, target, targetComp);

    internal void AcceptOffer(Entity<OfferItemComponent> ent)
        => base.AcceptOffer(ent);
}
