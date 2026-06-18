using Content.Shared.Imperial.Power.Events;
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
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not SupermatterConsoleBuiState smState)
            return;
        _menu?.Update(smState);
    }
}
