using System.Linq;
using Content.Shared.FixedPoint;
using Content.Server.Administration;
using Content.Shared.Administration;
using Robust.Shared.Console;
using Content.Shared.Imperial.ImperialStore;
using Content.Shared.Damage;
using Content.Shared.Mobs.Components;
using Content.Shared.Damage.Components;

namespace Content.Server.Imperial.ImperialLightning;

public sealed partial class ImperialLightningSystem
{
    [Dependency] private readonly IConsoleHost _consoleHost = default!;

    public void InitializeCommand()
    {
        _consoleHost.RegisterCommand(
            "set_non_cancelable_lightning_target",
            "Херачит молнией по выбранной сущности",
            "set_non_cancelable_lightning_target <uid>",
            SetTarget,
            SetTargetCompletions
        );
    }

    [AdminCommand(AdminFlags.Fun)]
    private void SetTarget(IConsoleShell shell, string argstr, string[] args)
    {
        if (args.Length != 1)
        {
            shell.WriteError("Argument length must be 1");
            return;
        }

        if (!NetEntity.TryParse(args[0], out var uidNet)) return;
        if (!TryGetEntity(uidNet, out var uid)) return;

        if (shell.Player?.AttachedEntity == null) return;

        var player = shell.Player.AttachedEntity.Value;

        SpawnLightningBetween(
            _transformSystem.GetWorldPosition(player),
            uid.Value
        );
    }

    private CompletionResult SetTargetCompletions(IConsoleShell shell, string[] args)
    {
        if (args.Length != 1) return CompletionResult.Empty;

        var query = EntityQueryEnumerator<TransformComponent, DamageableComponent, MobStateComponent>();
        var allEnts = new List<string>();

        while (query.MoveNext(out var ent, out var _, out _, out var _))
            allEnts.Add(ent.ToString());

        return CompletionResult.FromHintOptions(allEnts, "<uid>");
    }
}
