using System.Numerics;
using Content.Shared.Actions;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Imperial.XxRaay.Zero.KatanaRecall;
using Robust.Shared.Containers;

namespace Content.Server.Imperial.XxRaay.Zero.KatanaRecall;

/// <summary>
/// System that handles katana recall action.
/// When activated, finds the nearest KatanaZero entity and teleports it to the player, then puts it in their hands.
/// </summary>
public sealed class KatanaRecallSystem : EntitySystem
{
    [Dependency] private readonly SharedHandsSystem _handsSystem = default!;
    [Dependency] private readonly SharedTransformSystem _transformSystem = default!;
    [Dependency] private readonly SharedContainerSystem _containerSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        
        SubscribeAllEvent<KatanaRecallInstantActionEvent>(OnKatanaRecallAction);
    }

    private void OnKatanaRecallAction(KatanaRecallInstantActionEvent ev)
    {
        var performer = ev.Performer;

        if (!TryComp<HandsComponent>(performer, out var hands))
        {
            ev.Handled = true;
            return;
        }

        foreach (var handId in hands.SortedHands)
        {
            if (_handsSystem.TryGetHeldItem((performer, hands), handId, out var held) && held.HasValue)
            {
                var meta = MetaData(held.Value);
                if (meta.EntityPrototype?.ID == "KatanaZero")
                {
                    ev.Handled = true;
                    return;
                }
            }
        }

        EntityUid? nearestKatana = null;
        float nearestDistance = float.MaxValue;
        var performerPos = _transformSystem.GetWorldPosition(performer);

        var query = AllEntityQuery<MetaDataComponent>();
        while (query.MoveNext(out var uid, out var meta))
        {
            if (uid == performer)
                continue;

            if (meta.EntityPrototype?.ID != "KatanaZero")
                continue;

            if (_containerSystem.IsEntityInContainer(uid))
            {
                bool inPerformerHands = false;
                foreach (var handId in hands.SortedHands)
                {
                    if (_handsSystem.TryGetHeldItem((performer, hands), handId, out var held) && held == uid)
                    {
                        inPerformerHands = true;
                        break;
                    }
                }
                if (!inPerformerHands)
                    continue; 
            }

            var katanaPos = _transformSystem.GetWorldPosition(uid);
            var distance = Vector2.Distance(performerPos, katanaPos);

            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestKatana = uid;
            }
        }

        if (nearestKatana == null)
        {
            ev.Handled = true;
            return;
        }

        var performerCoords = Transform(performer).Coordinates;
        _transformSystem.SetCoordinates(nearestKatana.Value, performerCoords);

        _handsSystem.TryPickupAnyHand(performer, nearestKatana.Value, checkActionBlocker: false, animate: false);

        ev.Handled = true;
    }
}

