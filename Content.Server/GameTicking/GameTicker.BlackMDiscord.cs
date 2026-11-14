using Content.Server._BlackM.DiscordAuth;
using Robust.Shared.Player;

namespace Content.Server.GameTicking
{
    public sealed partial class GameTicker
    {
        public bool BlackMCheckDiscordJoin(ICommonSession player)
        {
            if (IoCManager.Resolve<Content.Corvax.Interfaces.Server.IServerDiscordAuthManager>() is not DiscordAuthManager auth)
                return true;

            if (auth.CanJoinRound(player.UserId))
                return true;

            auth.NotifyJoinBlocked(player);
            return false;
        }
    }
}
