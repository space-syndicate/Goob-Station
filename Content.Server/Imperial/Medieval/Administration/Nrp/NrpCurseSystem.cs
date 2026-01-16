using Content.Server.Administration.Managers;
using Robust.Shared.Network;
using Content.Server.Administration;
using Content.Shared.Database;
using Robust.Shared.Timing;

namespace Content.Server.Imperial.Medieval.Administration.Nrp;

public sealed partial class NrpCurseSystem : EntitySystem
{
    [Dependency] private readonly IBanManager _banManager = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly IPlayerLocator _locator = default!;

    private Dictionary<NetUserId, CursedEntity> _cursedEntities = new();

    public void CurseEntity(CursedEntity entity)
    {
        _cursedEntities.TryAdd(entity.PlayerId, entity);
    }

    public void RemoveCursedEntity(NetUserId id)
    {
        _cursedEntities.Remove(id);
    }

    private void Ban(NetUserId playerId, string playerName, NetUserId sender, string text, uint banMinutes)
    {
        _banManager.CreateServerBan(playerId,
            playerName,
            sender,
            null,
            null,
            banMinutes,
            NoteSeverity.Minor,
            text
        );
    }

    public async override void Update(float frameTime)
    {
        base.Update(frameTime);

        var toRemove = new List<NetUserId>();

        foreach (var (id, curse) in _cursedEntities)
        {
            if (_gameTiming.CurTime < curse.BanTime)
                continue;

            var located = await _locator.LookupIdAsync(id);

            if (located == null)
                return;

            Ban(id, located.Username, curse.Sender, curse.Text, curse.BanMinutes);
            toRemove.Add(id);
        }

        foreach (var id in toRemove)
            _cursedEntities.Remove(id);
    }
}

public sealed class CursedEntity
{
    public NetUserId PlayerId;
    public NetUserId Sender;
    public string Text;
    public TimeSpan BanTime;
    public uint BanMinutes;

    public CursedEntity(NetUserId playerId, NetUserId sender, string text, TimeSpan banTime, uint banMinutes)
    {
        PlayerId = playerId;
        Sender = sender;
        Text = text;
        BanTime = banTime;
        BanMinutes = banMinutes;
    }
}
