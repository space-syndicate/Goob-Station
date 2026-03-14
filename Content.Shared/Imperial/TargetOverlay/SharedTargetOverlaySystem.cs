using Content.Shared.Imperial.TargetOverlay.Events;
using Robust.Shared.Network;
using Robust.Shared.Player;

namespace Content.Shared.Imperial.TargetOverlay;


public abstract class SharedTargetOverlaySystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;


    #region Public API

    public void StartTargeting(EntityUid uid, EntityUid? sender = null, int maxTargetCount = 1, HashSet<string>? whiteListComponents = null, HashSet<string>? blackListComponents = null)
    {
        if (_net.IsClient)
            StartTargetingClient(uid, sender, maxTargetCount, whiteListComponents ?? new(), blackListComponents ?? new());
        else
            StartTargetingServer(uid, sender, maxTargetCount, whiteListComponents ?? new(), blackListComponents ?? new());
    }

    public void StopTargeting(EntityUid uid)
    {
        if (_net.IsClient)
            StopTargetingClient(uid);
        else
            StopTargetingServer(uid);
    }

    #endregion

    #region Helpers

    #region Client

    protected virtual void StartTargetingClient(EntityUid uid, EntityUid? sender, int maxTargetCount, HashSet<string> whiteListComponents, HashSet<string> blackListComponents)
    {
    }

    protected virtual void StopTargetingClient(EntityUid uid)
    {
    }

    #endregion

    #region Server

    protected virtual void StartTargetingServer(EntityUid uid, EntityUid? sender, int maxTargetCount, HashSet<string> whiteListComponents, HashSet<string> blackListComponents)
    {
        if (!TryComp<ActorComponent>(uid, out var actorComponent)) return;

        RaiseNetworkEvent(new AddTargetOverlayEvent()
        {
            Player = GetNetEntity(uid),
            Sender = GetNetEntity(sender),
            MaxTargetCount = maxTargetCount,
            WhiteListComponents = whiteListComponents,
            BlackListComponents = blackListComponents
        }, actorComponent.PlayerSession);
    }

    protected virtual void StopTargetingServer(EntityUid uid)
    {
        if (!TryComp<ActorComponent>(uid, out var actorComponent)) return;

        RaiseNetworkEvent(new RemoveTargetOverlayEvent()
        {
            Player = GetNetEntity(uid)
        }, actorComponent.PlayerSession);
    }

    #endregion

    #endregion
}
