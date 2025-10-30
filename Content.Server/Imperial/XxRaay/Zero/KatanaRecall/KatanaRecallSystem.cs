using Content.Shared.Hands.EntitySystems;
using Content.Shared.Imperial.XxRaay.Zero.KatanaRecall;
using Content.Shared.Popups;
using Robust.Server.GameObjects;

namespace Content.Server.Imperial.XxRaay.Zero.KatanaRecall;

/// <summary>
/// Server-side system for managing katana recall effects.
/// Handles teleporting the katana back to its owner's hand, similar to ninja system.
/// </summary>
public sealed class KatanaRecallSystem : SharedKatanaRecallSystem
{
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<KatanaRecallComponent, KatanaRecallActionEvent>(OnKatanaRecallAction);
    }

    private void OnKatanaRecallAction(Entity<KatanaRecallComponent> entity, ref KatanaRecallActionEvent args)
    {
        var (katana, component) = entity;
        var user = args.Performer;

        // Check cooldown
        if (IsOnCooldown(entity))
        {
            _popup.PopupEntity("Катана ещё не готова к возвращению!", user, user);
            return;
        }

        // Check if katana is in user's hands already
        if (_hands.IsHolding(user, katana))
        {
            _popup.PopupEntity("Катана уже в ваших руках!", user, user);
            return;
        }

        // Get katana position and check distance
        var katanaPos = _transform.GetWorldPosition(katana);
        var userPos = _transform.GetWorldPosition(user);
        var distance = (userPos - katanaPos).Length();

        // Check distance
        if (distance > component.MaxRecallDistance)
        {
            _popup.PopupEntity($"Катана слишком далеко! Максимальное расстояние: {component.MaxRecallDistance:F1}м", user, user);
            return;
        }

        // Try to teleport katana directly to user's hands (like ninja system)
        var message = _hands.TryPickupAnyHand(user, katana)
            ? "Катана вернулась в ваши руки!"
            : "Руки заняты!";
        
        _popup.PopupEntity(message, user, user);

        // Update cooldown only on successful recall
        if (_hands.IsHolding(user, katana))
        {
            component.LastRecallTime = GameTiming.CurTime;
            Dirty(katana, component);
        }
    }

}