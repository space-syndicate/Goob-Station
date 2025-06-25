using Content.Server.Administration.Logs;
using Content.Server.EUI;
using Content.Shared.Administration;
using Robust.Shared.Console;
using Content.Server.Administration;
using Content.Server.Imperial.Medieval.Administration.Nrp;

namespace Content.Server.Imperial.Medieval.Administration.Commands;

[AdminCommand(AdminFlags.Logs)]
public sealed class OpenNrpPanelCommand : IConsoleCommand
{
    public string Command => "nrppanel";
    public string Description => "Opens the nrp panel.";
    public string Help => $"Usage: {Command}";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (shell.Player is not { } player)
        {
            shell.WriteError(Loc.GetString("shell-cannot-run-command-from-server"));
            return;
        }

        var eui = IoCManager.Resolve<EuiManager>();
        var ui = new NrpPanelEui();
        eui.OpenEui(ui, player);
    }
}
