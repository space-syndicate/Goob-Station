using Robust.Shared.Console;
using Robust.Shared.Random;

namespace Content.Client.Imperial.RadialMenu;


public sealed class RadialContainerCommandTest : LocalizedCommands
{
    public override string Command => "radialtest";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var random = IoCManager.Resolve<IRobustRandom>();
        var sawmill = IoCManager.Resolve<ILogManager>().GetSawmill("radial menu");

        var radial = new RadialContainer();
        string[] tips =
        {
            "Бла-бла-бла ОЧЕНЬ крутой текст для проверки радиальной менюшки",
            "Вам лучше не знать, что тут было до апстрима. Вайтрим - шутники",
            "Тут был текст о том, что девелоперы вайтдрима не любят негров"
        };


        for (var i = 0; i < 8; i++)
        {
            var testButton = radial.AddButton("Action " + i, "/Textures/Interface/hammer_scaled.svg.192dpi.png");

            testButton.Tooltip = random.Pick(tips);
            testButton.Controller.OnPressed += (_) =>
            {
                sawmill.Debug("Radial Button Pressed");
            };
        }

        radial.CloseButton.Controller.OnPressed += (_) =>
        {
            sawmill.Debug("Close event for your own logic");
        };
        radial.OpenAttachedLocalPlayer();
    }
}
