// LuaWorld - This file is licensed under AGPLv3
// Copyright (c) 2025 LuaWorld
// See AGPLv3.txt for details.
using Robust.Shared.Console;

namespace Content.Server._Lua.Mapping;

public sealed class DrawLineCommand : IConsoleCommand
{
    public string Command => "drawline";
    public string Description => "Рисует крест квадратами до 1000 тайлов от вашей клетки (видно только вам)";
    public string Help => "drawline";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var sys = EntitySystem.Get<DrawLineSystem>();
        var player = shell.Player;
        if (player == null) return;
        sys.ToggleDrawForSession(player);
    }
}


