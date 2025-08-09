using Content.Server.Administration;
using Content.Shared._CorvaxGoob.Skills;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server._CorvaxGoob.Skills.Commands;

[AdminCommand(AdminFlags.Admin)]
public sealed class SetSkillsCommand : IConsoleCommand
{
    [Dependency] private readonly ILocalizationManager _localization = default!;
    [Dependency] private readonly IEntityManager _entity = default!;

    public string Command => "setskills";

    public string Description => "Sets whether skills system should be enabled.";

    public string Help => "setskills <enabled>";

    public void Execute(IConsoleShell shell, string arg, string[] args)
    {
        if (args.Length != 1)
        {
            shell.WriteError(_localization.GetString("shell-wrong-arguments-number"));
            return;
        }

        if (bool.TryParse(args[0], out var enabled))
        {
            shell.WriteError(_localization.GetString("shell-argument-must-be-boolean"));
            return;
        }

        _entity.System<SharedSkillsSystem>().Enabled = enabled;
    }

    public CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
            return CompletionResult.FromHintOptions(CompletionHelper.Booleans, "enabled");

        return CompletionResult.Empty;
    }
}
