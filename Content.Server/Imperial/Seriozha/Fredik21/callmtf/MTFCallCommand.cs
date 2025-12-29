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
    [Dependency] private readonly IEntityManager _entity = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    public override string Command => "callMTF";
    public override string Description => Loc.GetString("callertcommand-desc");
    public override string Help => Loc.GetString("callertcommand-help");

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
        {
            var options = _prototype
                .EnumeratePrototypes<MTFCallPresetPrototype>()
                .Select(p => new CompletionOption(p.ID, Loc.GetString(p.Desc)));

            return CompletionResult.FromHintOptions(options.OrderBy(x => x.Value, StringComparer.Ordinal).ToArray(), Loc.GetString("callertcommand-id-preset"));
        }
        return CompletionResult.Empty;
    }

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var audioSystem = _entity.System<SharedAudioSystem>();

        // Ошибка: Аргументы отсутствуют
        if (args.Length == 0)
        {
            shell.WriteError(Loc.GetString("callertcommand-error-args0"));
            var errorSound = new SoundPathSpecifier("/Audio/Imperial/ErtCall/noert.ogg", AudioParams.Default.WithVolume(-2f));
            audioSystem.PlayGlobal(errorSound, Filter.Broadcast(), true);
            return;
        }

        // Ошибка: Слишком много аргументов
        if (args.Length > 1)
        {
            shell.WriteError(Loc.GetString("callertcommand-error-args1"));
            return;
        }

        var protoId = args[0];
        if (!_prototype.TryIndex<MTFCallPresetPrototype>(protoId, out var proto))
        {
            shell.WriteError(Loc.GetString("callertcommand-error-prest-not-found", ("protoid", protoId)));
            return;
        }

        // 1. Воспроизведение звука через SoundSpecifier
        if (proto.AnnouncementSound != null)
        {
            audioSystem.PlayGlobal(proto.AnnouncementSound, Filter.Broadcast(), true);
        }

        // 2. Логика оповещения (Localization)
        if (!string.IsNullOrEmpty(proto.AnnouncementMessage))
        {
            var chatSystem = _entity.System<ChatSystem>();
            
            var message = Loc.GetString(proto.AnnouncementMessage);
            var sender = proto.AnnouncementSender != null 
                ? Loc.GetString(proto.AnnouncementSender) 
                : "Департамент безопасности Недр"; // Дефолтный отправитель
            
            chatSystem.DispatchGlobalAnnouncement(
                message, 
                sender: sender, 
                playSound: false, 
                colorOverride: Color.Gold);
        }
        
        // 3. Спавн
        var mtfSpawnSystem = _entity.System<CallMTFSystem>();
        if (mtfSpawnSystem.SpawnMTF(proto))
        {
            shell.WriteLine(Loc.GetString("callertcommand-success", ("protoid", protoId)));
        }
        else
        {
            shell.WriteError(Loc.GetString("callertcommand-error-when-load-grid"));
        }
    }
}
