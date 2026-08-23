using Content.Server.Imperial.XxRaay.Components;
using Content.Server.StationEvents.Events;
using Content.Shared.GameTicking.Components;
using Robust.Shared.Player;

namespace Content.Server.Imperial.XxRaay.Systems;

public sealed class AllAtOnceWormVentSpawnRule : StationEventSystem<AllAtOnceWormVentSpawnRuleComponent>
{
    [Dependency] private readonly WormVentSpawnRule _wormVentSpawn = default!;

    protected override void Started(EntityUid uid, AllAtOnceWormVentSpawnRuleComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);

        if (!_wormVentSpawn.TrySpawnWormsOnRandomStationVents(component.Prototype, component.Count, args.RuleId))
        {
            ForceEndSelf(uid, gameRule);
            return;
        }

        Announce(component);
    }

    private void Announce(AllAtOnceWormVentSpawnRuleComponent component)
    {
        var allPlayersInGame = Filter.Empty().AddWhere(GameTicker.UserHasJoinedGame);

        if (component.StartAnnouncement != null)
            ChatSystem.DispatchFilteredAnnouncement(allPlayersInGame, Loc.GetString(component.StartAnnouncement), playSound: false, colorOverride: component.StartAnnouncementColor);

        Audio.PlayGlobal(component.StartAudio, allPlayersInGame, true);
    }
}
