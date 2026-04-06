using Content.Shared.GameTicking;

namespace Content.Server.Imperial.SCP.NothingThere.Systems;

public sealed partial class ImperialNothingThereSystem
{
    public EntityUid? PausedMap { get; private set; }

    private void InitializeMap()
    {
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);
    }

    private void OnRoundRestart(RoundRestartCleanupEvent _)
    {
        if (PausedMap == null || !Exists(PausedMap))
            return;

        Del(PausedMap.Value);
    }
    private void EnsurePausedMap()
    {
        if (PausedMap != null && Exists(PausedMap))
            return;

        var mapUid = _map.CreateMap();
        _metaData.SetEntityName(mapUid, Loc.GetString("polymorph-paused-map-name"));
        _map.SetPaused(mapUid, true);
        PausedMap = mapUid;
    }
}
