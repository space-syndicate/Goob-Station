using System.Linq;
using Content.Server.Administration;
using Content.Shared.Administration;
using Robust.Shared.Console;
using Robust.Shared.Localization;

namespace Content.Server._BlackM.ChatImage;

[AdminCommand(AdminFlags.Admin)]
public sealed class SendImageCommand : IConsoleCommand
{
    [Dependency] private readonly IEntityManager _entMan = default!;

    public string Command => "sendimg";
    public string Description => Loc.GetString("cmd-sendimg-desc");
    public string Help => Loc.GetString("cmd-sendimg-help");

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length < 1)
        {
            shell.WriteError(Loc.GetString("cmd-sendimg-error-usage", ("help", Help)));
            return;
        }

        var caption = string.Join(' ', args.Skip(1));
        var adminName = shell.Player?.Name ?? "server";

        _entMan.System<ChatImageSystem>().SendFromUrl(shell, adminName, shell.Player?.UserId, args[0], caption);
    }

    public CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        return args.Length switch
        {
            1 => CompletionResult.FromHint(Loc.GetString("cmd-sendimg-hint-url")),
            2 => CompletionResult.FromHint(Loc.GetString("cmd-sendimg-hint-caption")),
            _ => CompletionResult.Empty
        };
    }
}
