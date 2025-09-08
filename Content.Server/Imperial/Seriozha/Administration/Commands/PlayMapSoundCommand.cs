using Content.Server.Audio;
using Content.Server.Administration;
using Content.Shared.Administration;
using Robust.Server.Player;
using Robust.Shared.Audio;
using Robust.Shared.Console;
using Robust.Shared.ContentPack;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server.Imperial.Seriozha.Administration.Commands;

[AdminCommand(AdminFlags.Fun)]
public sealed class PlayMapSoundCommand : IConsoleCommand
{
	[Dependency] private readonly IEntityManager _entManager = default!;
	[Dependency] private readonly IPlayerManager _playerManager = default!;
	[Dependency] private readonly IPrototypeManager _protoManager = default!;
	[Dependency] private readonly IResourceManager _res = default!;
	[Dependency] private readonly IMapManager _mapManager = default!;

	public string Command => "playmapsound";
	public string Description => "Plays a sound for all players on a single map.";
	public string Help => "playmapsound <path> [volume] [mapId]\nIf mapId is omitted, uses the executor's current map.";

	public void Execute(IConsoleShell shell, string argStr, string[] args)
	{
		if (args.Length == 0)
		{
			shell.WriteLine(Help);
			return;
		}
		var path = args[0];
		var audio = AudioParams.Default;
		if (args.Length >= 2)
		{
			if (int.TryParse(args[1], out var volume))
			{
				audio = audio.WithVolume(volume);
			}
			else
			{
				shell.WriteError($"Invalid volume of {args[1]} specified.");
				return;
			}
		}
		MapId? targetMap = null;
		if (args.Length >= 3)
		{
			if (int.TryParse(args[2], out var mapInt))
			{
				var mapId = new MapId(mapInt);
				if (_mapManager.MapExists(mapId))
					targetMap = mapId;
				else
				{
					shell.WriteError($"Map with id {mapInt} does not exist.");
					return;
				}
			}
			else
			{
				shell.WriteError($"Invalid map id: {args[2]}");
				return;
			}
		}
		if (targetMap == null)
		{
			if (shell.Player is not { AttachedEntity: { } ent })
			{
				shell.WriteError("No map specified and could not infer executor's map.");
				return;
			}

			if (_entManager.TryGetComponent(ent, out TransformComponent? xform))
			{
				targetMap = xform!.MapID;
			}
			else
			{
				shell.WriteError("No map specified and could not infer executor's map.");
				return;
			}
		}
		audio = audio.AddVolume(-8);
		var filter = Filter.BroadcastMap(targetMap.Value);
		_entManager.System<ServerGlobalSoundSystem>().PlayAdminGlobal(filter, path, audio, true);
	}

	public CompletionResult GetCompletion(IConsoleShell shell, string[] args)
	{
		if (args.Length == 1)
		{
			var hint = "<path>";
			var options = CompletionHelper.AudioFilePath(args[0], _protoManager, _res);
			return CompletionResult.FromHintOptions(options, hint);
		}

		if (args.Length == 2)
		{
			return CompletionResult.FromHint("[volume]");
		}

		if (args.Length == 3)
		{
			var options = CompletionHelper.MapIds(_entManager);
			return CompletionResult.FromHintOptions(options, "[mapId]");
		}

		return CompletionResult.Empty;
	}
}


