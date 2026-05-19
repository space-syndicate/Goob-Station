using Content.Client.Eui;

namespace Content.Client.Imperial.Vampire.UI;

public sealed class VampireDeadEui : BaseEui
{
    private readonly VampireDeadUI _menu;

    public VampireDeadEui()
    {
        _menu = new VampireDeadUI();
    }

    public override void Opened()
    {
        _menu.OpenCentered();
    }

    public override void Closed()
    {
        base.Closed();

        _menu.Close();
    }
}
