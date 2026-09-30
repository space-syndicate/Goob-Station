using Content.Shared._CorvaxNext.Alert.Click;
using Content.Shared.Alert;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Robust.Shared.Containers;
using Robust.Shared.Timing;

namespace Content.Shared._CorvaxGoob.OfferItem;

public abstract partial class SharedOfferItemSystem : EntitySystem
{
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private bool _acceptingOffer;

    [ValidatePrototypeId<AlertPrototype>]
    protected const string OfferAlert = "Offer";

    public override void Initialize()
    {
        SubscribeLocalEvent<OfferItemComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<OfferItemComponent, MoveEvent>(OnMove);

        SubscribeLocalEvent<OfferItemComponent, EntityTerminatingEvent>(OnOfferItemTerminating);

        SubscribeLocalEvent<OfferItemComponent, EntRemovedFromContainerMessage>(OnHandContainerRemoved);

        InitializeInteractions();

        SubscribeLocalEvent<OfferItemComponent, AcceptOfferAlertEvent>(OnClickAlertEvent);
    }

    private void OnClickAlertEvent(Entity<OfferItemComponent> ent, ref AcceptOfferAlertEvent ev)
    {
        if (ev.Handled)
            return;

        if (ev.AlertId != OfferAlert)
            return;

        ev.Handled = true;

        AcceptOffer(ent);
    }

    /// <summary>
    /// Вызывается при удалении любой стороны обмена. Очищает связанного партнёра,
    /// чтобы «висячие» ссылки <see cref="EntityUid"/> не попали в PVS-сериализацию.
    /// </summary>
    private void OnOfferItemTerminating(Entity<OfferItemComponent> ent, ref EntityTerminatingEvent args)
    {
        ResetOffer(ent, ent.Comp, showPopup: false);
    }

    /// <summary>
    /// Вызывается при удалении предмета из контейнера руки. Если это был предлагаемый предмет,
    /// оффер сбрасывается немедленно.
    /// </summary>
    private void OnHandContainerRemoved(EntityUid uid, OfferItemComponent comp, ref EntRemovedFromContainerMessage args)
    {
        if (args.Entity == EntityUid.Invalid)
            return;

        if (comp.Item != args.Entity)
            return;

        // Во время приёма подбор предмета тоже вынимает его из руки
        ResetOffer(uid, comp, showPopup: !_acceptingOffer);
    }

    /// <summary>
    /// Начинает новый оффер на указанной стороне: запоминает предлагаемый предмет и руку,
    /// в которой он находится.
    /// </summary>
    protected void StartOffer(EntityUid uid, OfferItemComponent comp, EntityUid item, string hand)
    {
        comp.Item = item;
        comp.Hand = hand;
        comp.IsInOfferMode = true;
        Dirty(uid, comp);
    }

    /// <summary>
    /// Связывает предлагающего и получателя в активный обмен одним вызовом.
    /// </summary>
    protected void LinkOffer(EntityUid user, OfferItemComponent userComp, EntityUid target, OfferItemComponent targetComp)
    {
        targetComp.IsInReceiveMode = true;
        targetComp.Target = user;
        Dirty(target, targetComp);

        userComp.Target = target;
        userComp.IsInOfferMode = false;
        Dirty(user, userComp);

        if (userComp.Item is not { } item)
            return;

        _popup.PopupPredicted(Loc.GetString("offer-item-try-give",
            ("item", Identity.Entity(item, EntityManager)),
            ("target", Identity.Entity(target, EntityManager))), user, user);
        _popup.PopupClient(Loc.GetString("offer-item-try-give-target",
            ("user", Identity.Entity(user, EntityManager)),
            ("item", Identity.Entity(item, EntityManager))), user, target);
    }

    /// <summary>
    /// Сбрасывает состояние оффера для <paramref name="uid"/> и, если есть связь, для партнёра.
    /// Это единственное место очистки полей компонента.
    /// </summary>
    protected void ResetOffer(EntityUid uid, OfferItemComponent comp, bool showPopup = true)
    {
        if (comp.Target is { Valid: true } target && TryComp<OfferItemComponent>(target, out var partner))
        {
            if (showPopup)
                ShowCancelPopups(uid, comp);

            partner.IsInOfferMode = false;
            partner.IsInReceiveMode = false;
            partner.Hand = null;
            partner.Target = null;
            partner.Item = null;
            Dirty(target, partner);
        }

        comp.IsInOfferMode = false;
        comp.IsInReceiveMode = false;
        comp.Hand = null;
        comp.Target = null;
        comp.Item = null;
        Dirty(uid, comp);
    }

    /// <summary>
    /// Показывает попапы «не отдал предмет». Определяет, кто держит предмет, а кто получатель.
    /// </summary>
    private void ShowCancelPopups(EntityUid uid, OfferItemComponent comp)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        EntityUid holder;
        EntityUid recipient;
        EntityUid item;

        if (comp.Item is { } selfItem)
        {
            holder = uid;
            recipient = comp.Target is { Valid: true } t ? t : uid;
            item = selfItem;
        }
        else if (comp.Target is { Valid: true } target && TryComp<OfferItemComponent>(target, out var partner) && partner.Item is { } partnerItem)
        {
            holder = target;
            recipient = uid;
            item = partnerItem;
        }
        else
        {
            return;
        }

        _popup.PopupClient(Loc.GetString("offer-item-no-give",
            ("item", Identity.Entity(item, EntityManager)),
            ("target", Identity.Entity(recipient, EntityManager))), holder, holder);
        _popup.PopupEntity(Loc.GetString("offer-item-no-give-target",
            ("user", Identity.Entity(holder, EntityManager)),
            ("item", Identity.Entity(item, EntityManager))), holder, recipient);
    }

    /// <summary>
    /// Принимает оффер и подбирает предмет. При успехе обе стороны сбрасываются без попапов
    /// (передача уже состоялась, показывать «не отдал» не нужно).
    /// </summary>
    protected void AcceptOffer(Entity<OfferItemComponent> ent)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        if (ent.Comp.Target is not { Valid: true } target)
            return;

        if (!TryComp<OfferItemComponent>(target, out var offerItem))
            return;

        if (offerItem.Hand is null || offerItem.Item is not { } item)
            return;

        if (!TryComp<HandsComponent>(ent, out var hands))
            return;

        bool pickedUp;
        _acceptingOffer = true;
        try
        {
            pickedUp = _hands.TryPickup(ent, item, handsComp: hands);
        }
        finally
        {
            _acceptingOffer = false;
        }

        if (!pickedUp)
        {
            _popup.PopupClient(Loc.GetString("offer-item-full-hand"), ent, ent);
            return;
        }

        _popup.PopupClient(Loc.GetString("offer-item-give",
            ("item", Identity.Entity(item, EntityManager)),
            ("target", Identity.Entity(ent, EntityManager))), target, target);

        _popup.PopupPredicted(Loc.GetString("offer-item-give-other",
                ("user", Identity.Entity(target, EntityManager)),
                ("item", Identity.Entity(item, EntityManager)),
                ("target", Identity.Entity(ent, EntityManager))),
            target,
            ent);

        // Передача состоялась - сбрасываем обе стороны.
        ResetOffer(ent, ent.Comp, showPopup: false);
    }

    private void OnInteractUsing(EntityUid uid, OfferItemComponent component, ref InteractUsingEvent args)
    {
        if (!TryComp<OfferItemComponent>(args.User, out var userComp))
            return;

        if (args.User == uid || component.IsInReceiveMode || !userComp.IsInOfferMode ||
            userComp.IsInReceiveMode && userComp.Target != uid)
        {
            return;
        }

        LinkOffer(args.User, userComp, uid, component);

        args.Handled = true;
    }

    private void OnMove(EntityUid uid, OfferItemComponent component, MoveEvent args)
    {
        if (component.Target is not { Valid: true } target)
            return;

        var targetCoords = Transform(target).Coordinates;

        if (_transform.InRange(args.NewPosition, targetCoords, component.MaxOfferDistance))
            return;

        ResetOffer(uid, component);
    }

    /// <summary>
    /// Возвращает true, если <see cref="OfferItemComponent.IsInOfferMode"/> = true
    /// </summary>
    protected bool IsInOfferMode(EntityUid? entity, OfferItemComponent? component = null)
    {
        return entity is not null && Resolve(entity.Value, ref component, false) && component.IsInOfferMode;
    }
}
