using Robust.Shared.Console;

namespace Content.Client._CorvaxGoob.Mapping;

public sealed class DrawLineCommand : LocalizedCommands
{
    public override string Command => "drawline";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (shell.Player == null)
            return;

        if (EntitySystem.TryGet<DrawLineSystem>(out var sys))
            sys.ToggleDrawLine();
    }
}
