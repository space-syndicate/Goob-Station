using Content.Server.Administration.Logs;
using Content.Server.EUI;
using Content.Shared.Administration;
using Robust.Shared.Console;
using Content.Server.Administration;
using Content.Server.Imperial.Medieval.Administration.Nrp;
using Content.Shared.CCVar;
using Content.Shared.Imperial.Medieval.Administration.Nrp;
using Robust.Shared.Configuration;

namespace Content.Server.Imperial.Medieval.Administration.Commands;

[AdminCommand(AdminFlags.Ban)]
public sealed class NrpPardonCommand : IConsoleCommand
{
    public string Command => "nrppardon";
    public string Description => "NRP unwarn";
    public string Help => $"Usage: {Command} <name or user ID>";

    [Dependency] private readonly IPlayerLocator _locator = default!;
    [Dependency] private readonly IEntitySystemManager _entitySystemManager = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    private NrpMessagesSystem _nrpSystem = default!;

    public async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (!_cfg.GetCVar(NrpCCVars.NrpPanelEnabled))
        {
            shell.WriteLine("Command is disabled on server");
            return;
        }
        _nrpSystem = _entitySystemManager.GetEntitySystem<NrpMessagesSystem>();

        if (shell.Player is not { } player)
        {
            shell.WriteError(Loc.GetString("shell-cannot-run-command-from-server"));
            return;
        }

        string target;
        string reason;

        switch (args.Length)
        {
            case 1:
                target = args[0];
                break;
            default:
                shell.WriteLine("Invalid arguments");
                return;
        }

        var located = await _locator.LookupIdByNameOrIdAsync(target);

        if (located == null)
        {
            shell.WriteError("Invalid target");
            return;
        }

        var targetUid = located.UserId;
        var targetName = located.Username;

        await _nrpSystem.RemovePlayerNrpViolation(targetUid);
    }
}
