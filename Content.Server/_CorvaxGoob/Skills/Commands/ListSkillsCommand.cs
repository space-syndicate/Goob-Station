using System.Linq;
using System.Text;
using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Robust.Shared.Console;

namespace Content.Server._CorvaxGoob.Skills.Commands;

[AdminCommand(AdminFlags.Admin)]
public sealed class ListSkillsCommand : LocalizedEntityCommands
{
    [Dependency] private readonly ILocalizationManager _localization = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;

    public override string Command => "listskills";

    public override void Execute(IConsoleShell shell, string arg, string[] args)
    {
        if (args.Length != 1)
        {
            shell.WriteError(_localization.GetString("shell-wrong-arguments-number"));
            return;
        }

        if (!NetEntity.TryParse(args[0], out var id))
        {
            shell.WriteError(_localization.GetString("shell-entity-uid-must-be-number"));
            return;
        }

        if (!EntityManager.TryGetEntity(id, out var entity))
        {
            shell.WriteError(_localization.GetString("shell-invalid-entity-id"));
            return;
        }

        if (!_mind.TryGetMind(entity.Value, out _, out var mind))
        {
            shell.WriteError(_localization.GetString("shell-invalid-entity-id"));
            return;
        }

        StringBuilder builder = new();

        builder.AppendJoin('\n', mind.Skills.Order());

        builder.Append('\n');

        shell.WriteLine(builder.ToString());
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
        {
            return CompletionResult.FromHintOptions(
                CompletionHelper.Components<MindContainerComponent>(args[0], EntityManager, 1000).Where(option =>
                !EntityManager.HasComponent<MindComponent>(new EntityUid(int.Parse(option.Value))) &&
                EntityManager.GetComponent<MindContainerComponent>(new EntityUid(int.Parse(option.Value))).HasMind),
                _localization.GetString("shell-argument-net-entity"));
        }
        return CompletionResult.Empty;
    }
}
