using Content.Shared._BlackM.Rappel;
using Robust.Client.UserInterface;

namespace Content.Client._BlackM.Rappel.UI;

public sealed class RappelConsoleBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private RappelConsoleWindow? _window;

    public RappelConsoleBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<RappelConsoleWindow>();
        _window.OnSelectPoint += pos => SendMessage(new RappelConsoleSelectPointMessage(pos));
        _window.OnClearPoint += () => SendMessage(new RappelConsoleClearPointMessage());
        _window.OnMoveCamera += pos => SendMessage(new RappelConsoleMoveCameraMessage(pos));
        _window.OnRecenter += () => SendMessage(new RappelConsoleRecenterMessage());
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is RappelConsoleBoundUserInterfaceState cast)
            _window?.UpdateState(cast);
    }
}
