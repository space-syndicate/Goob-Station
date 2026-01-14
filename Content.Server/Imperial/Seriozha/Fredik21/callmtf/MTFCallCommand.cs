using Robust.Shared.Console;
using Content.Shared.Administration;
using Content.Server.Administration;
using Robust.Shared.Prototypes;
using System.Linq;
using Robust.Shared.Audio.Systems;
using Content.Server.Chat.Systems;
using Robust.Shared.Player;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.EntitySerialization.Systems; // <--- ПРАВИЛЬНЫЙ ПУТЬ ДЛЯ MapLoaderSystem
using Robust.Shared.EntitySerialization;

namespace Content.Server.Imperial.MTFCall;

[AdminCommand(AdminFlags.Admin)]
public sealed class CallMTF : LocalizedCommands
{
    [Dependency] private readonly IEntityManager _entManager = default!;
    [Dependency] private readonly IPrototypeManager _protoManager = default!;

    public override string Command => "callMTF";
    public override string Description => Loc.GetString("callertcommand-desc");
    public override string Help => Loc.GetString("callertcommand-help");

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
        {
            var options = _protoManager
                .EnumeratePrototypes<MTFCallPresetPrototype>()
                .Select(p => new CompletionOption(p.ID, Loc.GetString(p.Desc)));

            return CompletionResult.FromHintOptions(options.OrderBy(x => x.Value).ToArray(), Loc.GetString("callertcommand-id-preset"));
        }
        return CompletionResult.Empty;
    }

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 1)
        {
            shell.WriteError(Loc.GetString("callertcommand-error-args0"));
            return;
        }

        var protoId = args[0];
        if (!_protoManager.TryIndex<MTFCallPresetPrototype>(protoId, out var proto))
        {
            shell.WriteError(Loc.GetString("callertcommand-error-prest-not-found", ("protoid", protoId)));
            return;
        }

        // 1. Анонсы (Звук и Текст)
        if (proto.AnnouncementSound != null)
            _entManager.System<SharedAudioSystem>().PlayGlobal(proto.AnnouncementSound, Filter.Broadcast(), true);

        if (proto.AnnouncementMessage != null)
        {
            var sender = proto.AnnouncementSender != null 
                ? Loc.GetString(proto.AnnouncementSender) 
                : Loc.GetString("announcementSender-MTF-NDA");
                
            _entManager.System<ChatSystem>().DispatchGlobalAnnouncement(
                Loc.GetString(proto.AnnouncementMessage), 
                sender: sender, 
                playSound: false, 
                colorOverride: Color.Gold);
        }

        // 2. Логика загрузки грида
        var mapSystem = _entManager.System<SharedMapSystem>();
        var mapLoader = _entManager.System<MapLoaderSystem>();

        // Создаем новую карту
        var shuttleMapUid = mapSystem.CreateMap();
        
        if (!_entManager.TryGetComponent<MapComponent>(shuttleMapUid, out var mapComp))
        {
            shell.WriteError("Failed to get MapComponent from created map.");
            return;
        }

        var mapId = mapComp.MapId;
        var options = new DeserializationOptions { InitializeMaps = true };

        // Загружаем грид из прототипа
        if (mapLoader.TryLoadGrid(mapId, proto.Path, out _, options))
        {
            shell.WriteLine(Loc.GetString("callertcommand-success", ("protoid", protoId)));
        }
        else
        {
            shell.WriteError(Loc.GetString("callertcommand-error-when-load-grid"));
            mapSystem.DeleteMap(mapId);
        }
    }
}
