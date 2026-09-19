using Robust.Shared.Player;

namespace Content.Server._CorvaxGoob.Events;

public sealed class GhostJoinLobbyRequestEvent : EntityEventArgs
{
    public readonly ICommonSession Session;

    public GhostJoinLobbyRequestEvent(ICommonSession session)
    {
        Session = session;
    }
}