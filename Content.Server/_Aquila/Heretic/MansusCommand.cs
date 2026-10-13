using Content.Server.Administration;
using Content.Server.Station.Systems;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server._Aquila.Heretic;

[AdminCommand(AdminFlags.Debug)]
public sealed class MansusCommand : LocalizedEntityCommands
{
    [Dependency] private readonly MansusRealmSystem _mansus = default!;
    [Dependency] private readonly StationSystem _station = default!;

    public override string Command => "mansus";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        EntityUid target;
        if (args.Length == 0)
        {
            if (shell.Player?.AttachedEntity is not { } attached)
            {
                shell.WriteError(Loc.GetString("cmd-mansus-no-target"));
                return;
            }

            target = attached;
        }
        else if (!NetEntity.TryParse(args[0], out var netTarget) || !EntityManager.TryGetEntity(netTarget, out var parsed))
        {
            shell.WriteError(Loc.GetString("shell-entity-uid-must-be-number"));
            return;
        }
        else
        {
            target = parsed.Value;
        }

        if (!_mansus.TrySendToMansus(target, _station.GetOwningStation(target)))
            shell.WriteError(Loc.GetString("cmd-mansus-failed"));
    }
}
