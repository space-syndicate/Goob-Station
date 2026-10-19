using Content.Server._Aquila.Economy.Systems;
using Content.Server.Administration;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server._Aquila.Economy;

[AdminCommand(AdminFlags.Admin)]
public sealed class BankPanelCommand : LocalizedEntityCommands
{
    [Dependency] private readonly BankAdminSystem _bankAdmin = default!;

    public override string Command => "bankpanel";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (shell.Player is not { } player)
        {
            shell.WriteError(Loc.GetString("shell-cannot-run-command-from-server"));
            return;
        }

        int? number = null;
        if (args.Length > 0)
        {
            if (!int.TryParse(args[0], out var parsed))
            {
                shell.WriteError(Loc.GetString("shell-argument-must-be-number"));
                return;
            }

            number = parsed;
        }

        _bankAdmin.OpenPanel(player, number);
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        return args.Length == 1
            ? CompletionResult.FromHint(Loc.GetString("cmd-bankpanel-hint"))
            : CompletionResult.Empty;
    }
}
