using Content.Shared._Aquila.Economy;
using Robust.Client.UserInterface;

namespace Content.Client._Aquila.Economy;

public sealed class AtmBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    private AtmWindow? _window;

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<AtmWindow>();
        _window.Title = EntMan.GetComponent<MetaDataComponent>(Owner).EntityName;
        _window.OnWithdraw += amount => SendMessage(new AtmWithdrawMessage(amount));
        _window.OnOpenAccount += () => SendMessage(new AtmOpenAccountMessage());
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is AtmUiState atmState)
            _window?.UpdateState(atmState);
    }
}
