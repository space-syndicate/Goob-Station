using System.Linq;
using Content.Server.Administration;
using Content.Server.Administration.Logs;
using Content.Server.Audio;
using Content.Shared.Administration;
using Content.Shared.Database;
using Robust.Shared.Audio;
using Robust.Shared.Console;
using Robust.Shared.ContentPack;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server._Aquila.Audio;

[AdminCommand(AdminFlags.Fun)]
public sealed class PlayMapSoundCommand : LocalizedEntityCommands
{
    [Dependency] private readonly IAdminLogManager _adminLog = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IResourceManager _res = default!;
    [Dependency] private readonly ServerGlobalSoundSystem _globalSound = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;

    private const float AdminVolumeOffset = -8f;

    public override string Command => "playmapsound";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length is < 1 or > 3)
        {
            shell.WriteError(Loc.GetString("shell-wrong-arguments-number"));
            return;
        }

        MapId mapId;
        if (args.Length > 1)
        {
            if (!int.TryParse(args[1], out var mapNumber))
            {
                shell.WriteError(Loc.GetString("shell-argument-must-be-number"));
                return;
            }

            mapId = new MapId(mapNumber);
        }
        else if (shell.Player?.AttachedEntity is { } player)
        {
            mapId = EntityManager.GetComponent<TransformComponent>(player).MapID;
        }
        else
        {
            shell.WriteError(Loc.GetString("cmd-playmapsound-no-map"));
            return;
        }

        if (!_map.MapExists(mapId))
        {
            shell.WriteError(Loc.GetString("cmd-playmapsound-map-not-found", ("map", mapId.ToString())));
            return;
        }

        var audio = AudioParams.Default;
        if (args.Length > 2)
        {
            if (!int.TryParse(args[2], out var volume))
            {
                shell.WriteError(Loc.GetString("play-global-sound-command-volume-parse", ("volume", args[2])));
                return;
            }

            audio = audio.WithVolume(volume);
        }

        var filter = Filter.Empty().AddInMap(mapId, EntityManager);
        _globalSound.PlayAdminGlobal(filter, args[0], audio.AddVolume(AdminVolumeOffset));

        var players = string.Join(", ", filter.Recipients.Select(session => session.Name).Order());
        shell.WriteLine(filter.Count == 0
            ? Loc.GetString("cmd-playmapsound-success-empty", ("map", mapId.ToString()))
            : Loc.GetString("cmd-playmapsound-success", ("map", mapId.ToString()), ("players", players)));
        _adminLog.Add(LogType.AdminCommands, LogImpact.High,
            $"{shell.Player?.Name ?? "Server"} played sound {args[0]} on map {mapId} for: {players}");
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        return args.Length switch
        {
            1 => CompletionResult.FromHintOptions(CompletionHelper.AudioFilePath(args[0], _proto, _res),
                Loc.GetString("play-global-sound-command-arg-path")),
            2 => CompletionResult.FromHintOptions(CompletionHelper.MapIds(EntityManager),
                Loc.GetString("cmd-playmapsound-arg-map")),
            3 => CompletionResult.FromHint(Loc.GetString("play-global-sound-command-arg-volume")),
            _ => CompletionResult.Empty,
        };
    }
}
