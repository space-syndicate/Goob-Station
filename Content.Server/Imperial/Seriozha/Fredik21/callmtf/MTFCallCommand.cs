using Robust.Shared.Console;
using Content.Shared.Administration;
using Content.Server.Administration;
using Robust.Shared.Prototypes;
using System.Linq;
using Robust.Shared.Audio;
using Robust.Shared.Player;
using Robust.Shared.Audio.Systems;
using Content.Server.Chat.Systems;
using Robust.Shared.IoC;
using Robust.Shared.Localization;

namespace Content.Server.Imperial.MTFCall;

[AdminCommand(AdminFlags.Admin)]
public sealed class CallMTF : LocalizedCommands
{
    public override string Description => Loc.GetString("callertcommand-desc");
    public override string Help => Loc.GetString("callertcommand-help");
    [Dependency] private readonly IEntityManager _entity = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    public override string Command => "callMTF";

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
        {
            var options = _prototype
                .EnumeratePrototypes<MTFCallPresetPrototype>()
                .Select(p => new CompletionOption(p.ID, p.Desc));

            return CompletionResult.FromHintOptions(options.OrderBy(x => x.Value, StringComparer.Ordinal).ToArray(), Loc.GetString("callertcommand-id-preset"));
        }

        return CompletionResult.Empty;
    }

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length == 0)
        {
            shell.WriteError(Loc.GetString("callertcommand-error-args0"));
            _entity.System<SharedAudioSystem>().PlayGlobal("/Audio/Imperial/ErtCall/noert.ogg", Filter.Broadcast(), true, AudioParams.Default.WithVolume(-2f));
            return;
        }
        if (args.Length > 1)
        {
            shell.WriteError(Loc.GetString("callertcommand-error-args1"));
            return;
        }
        
        var protoId = args[0]; 
        
        var MTFSpawnSystem = _entity.System<CallMTFSystem>();
        if (!_prototype.TryIndex<MTFCallPresetPrototype>(protoId, out var proto))
        {
            shell.WriteError(Loc.GetString("callertcommand-error-prest-not-found", ("protoid", protoId)));
            return;
        }

        // --- Логика музыки ---
        var audioSystem = _entity.System<SharedAudioSystem>();
        if (!string.IsNullOrEmpty(proto.MusicPath))
        {
            audioSystem.PlayGlobal(proto.MusicPath, Filter.Broadcast(), true, proto.MusicParams);
        }

        // --- Логика оповещения (ИЗМЕНЕНО) ---
        if (!string.IsNullOrEmpty(proto.AnnouncementMessage))
        {
            var chatSystem = _entity.System<ChatSystem>();
            
            // Если в прототипе не указан отправитель, используем ваш вариант по умолчанию
            var sender = proto.AnnouncementSender ?? "Департамент безопасности Недр";
            
            chatSystem.DispatchGlobalAnnouncement(
                proto.AnnouncementMessage, 
                sender: sender, 
                playSound: false, 
                colorOverride: Color.Gold);
        }
        
        if (MTFSpawnSystem.SpawnMTF(proto))
        {
            return;
        }
        else
        {
            shell.WriteError(Loc.GetString("callertcommand-error-when-load-grid"));
            return;
        }
    }
}
