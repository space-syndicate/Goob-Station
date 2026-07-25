using System.Linq;
using Content.Server.Administration;
using SkillTypes = Content.Shared._CorvaxGoob.Skills.Skills;
using Content.Shared.Administration;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Robust.Shared.Console;

namespace Content.Server._CorvaxGoob.Skills.Commands;

[AdminCommand(AdminFlags.Admin)]
public sealed class RevokeSkillCommand : LocalizedEntityCommands
{
    [Dependency] private readonly ILocalizationManager _localization = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly SkillsSystem _skills = default!;

    public override string Command => "revokeskill";

    public override void Execute(IConsoleShell shell, string arg, string[] args)
    {
        if (args.Length < 2)
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

        if (!_mind.TryGetMind(entity.Value, out _, out var _))
        {
            shell.WriteError(_localization.GetString("shell-invalid-entity-id"));
            return;
        }

        var skills = new HashSet<SkillTypes>();

        for (int i = 1; i < args.Length; i++)
        {
            if (!Enum.TryParse<SkillTypes>(args[i], out var skill))
            {
                shell.WriteError(Loc.GetString("cmd-revokeskill-not-a-skill-type", ("args", args[i])));
                return;
            }
            skills.Add(skill);
        }

        _skills.RevokeSkill(entity.Value, skills);
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

        var component = int.TryParse(args[0], out var id)
            ? EntityManager.TryGetEntity(new(id), out var entity)
                ? _mind.TryGetMind(entity.Value, out _, out var comp)
                    ? comp
                    : null
                : null
            : null;

        var existingSkills = component?.Skills ?? new HashSet<SkillTypes>();

        var alreadyEnteredSkills = new HashSet<SkillTypes>();
        for (int i = 1; i < args.Length - 1; i++)
        {
            if (Enum.TryParse<SkillTypes>(args[i], out var skill))
                alreadyEnteredSkills.Add(skill);
        }

        return CompletionResult.FromOptions(existingSkills
            .Where(skill => !alreadyEnteredSkills.Contains(skill))
            .Select(skill => skill.ToString())
            .Where(name => name.StartsWith(args[^1], StringComparison.OrdinalIgnoreCase))
            .Select(name => new CompletionOption(name)));
    }
}
