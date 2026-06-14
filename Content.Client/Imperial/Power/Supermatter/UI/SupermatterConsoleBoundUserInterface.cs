using Content.Shared.Imperial.Power;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client.Imperial.Power.Supermatter.UI;

[UsedImplicitly]
public sealed class SupermatterConsoleBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    [ViewVariables]
    private SupermatterConsoleWindow? _menu;

    protected override void Open()
    {
        base.Open();

        _menu = this.CreateWindow<SupermatterConsoleWindow>();
        _menu.SetEntity(Owner);
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        var castState = (SupermatterConsoleBuiState) state;
        _menu?.Update(castState);
    }
}
