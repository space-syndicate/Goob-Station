using Content.Client.UserInterface.Controls;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Console;

namespace Content.Client._CorvaxGoob.MindLink;

/// <summary>
/// Opens a client-only preview of the MindLink target list.
/// </summary>
public sealed class MindLinkUiTestCommand : IConsoleCommand
{
    private const int DefaultTargetCount = 20;
    private const int MaxTargetCount = 200;
    private const float MaxTargetListHeight = 500f;

    public string Command => "mindlink_ui_test";
    public string Description => "Opens a client-only MindLink target list preview.";
    public string Help => $"Usage: {Command} [target count, 1-{MaxTargetCount}]";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length > 1
            || args.Length == 1 && (!int.TryParse(args[0], out var parsed) || parsed < 1 || parsed > MaxTargetCount))
        {
            shell.WriteError(Help);
            return;
        }

        var targetCount = args.Length == 1 ? int.Parse(args[0]) : DefaultTargetCount;
        var window = new FancyWindow
        {
            MinWidth = 320,
            Title = Loc.GetString("mind-link-target-window-title"),
        };
        var list = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            HorizontalExpand = true,
        };
        list.AddChild(new Button { Text = Loc.GetString("mind-link-all-targets") });

        for (var i = 1; i <= targetCount; i++)
        {
            var button = new Button { Text = $"Test target {i:00}", HorizontalExpand = true };
            if (i > 5)
            {
                list.AddChild(button);
                continue;
            }

            var row = new BoxContainer
            {
                Orientation = BoxContainer.LayoutOrientation.Horizontal,
                HorizontalExpand = true,
            };
            row.AddChild(button);
            row.AddChild(new Button
            {
                Text = "×",
                ToolTip = Loc.GetString("mind-link-disconnect"),
            });
            list.AddChild(row);
        }

        var scroll = new ScrollContainer
        {
            MaxHeight = MaxTargetListHeight,
            HorizontalExpand = true,
            HScrollEnabled = false,
            ReserveScrollbarSpace = true,
            ReturnMeasure = true,
        };
        scroll.AddChild(list);
        window.ContentsContainer.AddChild(scroll);
        window.OpenCentered();
    }
}
