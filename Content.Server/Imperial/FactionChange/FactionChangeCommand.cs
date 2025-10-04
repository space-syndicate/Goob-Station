using Content.Shared.Administration;
using Robust.Shared.Console;
using Content.Shared.NPC.Systems;
using Content.Server.Administration;
using Content.Shared.NPC.Components;
using Robust.Shared.GameObjects;
using Content.Shared.NPC.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Server.Imperial.FactionChange
{
    [AdminCommand(AdminFlags.Admin)]
    public sealed class FactionChangeCommand : IConsoleCommand
    {
        [Dependency] private readonly IEntityManager _entManager = default!;
        [Dependency] private readonly IPrototypeManager _proto = default!;

        public string Command => "changefaction";
        public string Description => "changefaction-desc";
        public string Help => "changefaction-help";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length != 2)
            {
                shell.WriteError(Loc.GetString("cf-shell-wrong-arguments-number"));
                shell.WriteError(Loc.GetString(Help));
                return;
            }

            if (!NetEntity.TryParse(args[0], out var entityIdNet))
            {
                shell.WriteError(Loc.GetString("cf-shell-entity-uid-must-be-number"));
                return;
            }
            var changedFaction = args[1];

            var entityId = _entManager.GetEntity(entityIdNet);
            if (_entManager.HasComponent<NpcFactionMemberComponent>(entityId))
            {
                var target = entityId;
                var faction = changedFaction;
                var npcFactionSystem = _entManager.System<NpcFactionSystem>();

                if (!_proto.HasIndex<NpcFactionPrototype>(faction))
                {
                    shell.WriteError(Loc.GetString("cf-shell-faction-doesnt-exist", ("faction", faction)));
                    return;
                }
                npcFactionSystem.ClearFactions(target, true);
                npcFactionSystem.AddFaction(target, faction, false);
                shell.WriteLine(Loc.GetString("cf-command-enter-successful", ("faction", faction)));
            }
            else
            {
                shell.WriteError(Loc.GetString("cf-shell-entity-there-is-no-faction-component"));
                return;
            }
        }
    }
}

