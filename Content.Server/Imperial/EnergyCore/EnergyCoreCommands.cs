using Content.Shared.Administration;
using Robust.Shared.Console;
using Content.Server.Administration;
using Robust.Shared.GameObjects;
using Content.Server.Imperial.EnergyCore;
using Content.Server.Imperial.EnergyCore.Components;

namespace Content.Server.Imperial.EnergyCore.Commands
{
    [AdminCommand(AdminFlags.Admin)]
    public sealed class RestoreCoreCommand : LocalizedCommands
    {
        [Dependency] private readonly IEntityManager _entManager = default!;
        public override string Command => "restorecore";
        public override string Description => Loc.GetString("restorecore-desc");
        public string Help => Loc.GetString("restorecore-help");

        public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
        {
            if (args.Length == 1)
                return CompletionResult.FromHintOptions(CompletionHelper.Components<EnergyCoreComponent>(args[0], _entManager), Loc.GetString("core-hint"));
            if (args.Length == 2)
                return CompletionResult.FromHintOptions(CompletionHelper.Components<CoreAccessComputerComponent>(args[1], _entManager), Loc.GetString("terminal-hint"));
            if (args.Length == 3)
                return CompletionResult.FromHintOptions(CompletionHelper.Booleans, Loc.GetString("restorecore-hint-bool"));
            else return CompletionResult.Empty;
        }
        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (!NetEntity.TryParse(args[0], out var entityIdNet))
            {
                shell.WriteError(Loc.GetString("shell-entity-uid-must-be-number"));
                return;
            }
            var entityId = _entManager.GetEntity(entityIdNet);

            if (!NetEntity.TryParse(args[1], out var entityIdtNet))
            {
                shell.WriteError(Loc.GetString("shell-entity-uid-must-be-number"));
                return;
            }
            var entityIdt = _entManager.GetEntity(entityIdtNet);

            if (!bool.TryParse(args[2], out var announce))
            {
                shell.WriteLine(Loc.GetString("shell-argument-must-be-boolean"));
                return;
            }

            var terminal = _entManager.System<CoreAccessComputerSystem>();
            terminal.ResetTerminal(entityIdt);
            var core = _entManager.System<EnergyCoreSystem>();
            core.RestoreCore(entityId, announce);
        }
    }

    [AdminCommand(AdminFlags.Admin)]
    public sealed class CorearmCommand : LocalizedCommands
    {
        [Dependency] private readonly IEntityManager _entManager = default!;
        public override string Command => "corearm";
        public override string Description => Loc.GetString("corearm-desc");
        public string Help => Loc.GetString("corearm-help");

        public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
        {
            if (args.Length != 1)
                return CompletionResult.Empty;
            return CompletionResult.FromHintOptions(CompletionHelper.Components<EnergyCoreComponent>(args[0], _entManager), Loc.GetString("core-hint"));
        }
        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (!NetEntity.TryParse(args[0], out var entityIdNet))
            {
                shell.WriteError(Loc.GetString("shell-entity-uid-must-be-number"));
                return;
            }
            var entityId = _entManager.GetEntity(entityIdNet);
            var core = _entManager.System<EnergyCoreSystem>();
            core.Corearm(entityId);
        }
    }
}

