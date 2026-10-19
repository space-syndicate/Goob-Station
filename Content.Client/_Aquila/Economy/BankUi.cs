using Content.Client.UserInterface.Fragments;
using Content.Shared._Aquila.Economy;
using Content.Shared.CartridgeLoader;
using Robust.Client.UserInterface;

namespace Content.Client._Aquila.Economy;

public sealed partial class BankUi : UIFragment
{
    private BankUiFragment? _fragment;

    public override Control GetUIFragmentRoot()
    {
        return _fragment!;
    }

    public override void Setup(BoundUserInterface userInterface, EntityUid? fragmentOwner)
    {
        _fragment = new BankUiFragment();
        _fragment.OnTransfer += (number, amount) =>
            userInterface.SendMessage(new CartridgeUiMessage(new BankTransferMessageEvent(number, amount)));
        _fragment.OnBonus += (number, amount, comment) =>
            userInterface.SendMessage(new CartridgeUiMessage(new BankBonusMessageEvent(number, amount, comment)));
        _fragment.OnFine += (number, amount, comment) =>
            userInterface.SendMessage(new CartridgeUiMessage(new BankFineMessageEvent(number, amount, comment)));
        _fragment.OnRequestReply += (id, approve) =>
            userInterface.SendMessage(new CartridgeUiMessage(new BankRequestReplyMessageEvent(id, approve)));
        _fragment.OnRevokeFine += id =>
            userInterface.SendMessage(new CartridgeUiMessage(new BankFineRevokeMessageEvent(id)));
        _fragment.OnOpenAccount += () =>
            userInterface.SendMessage(new CartridgeUiMessage(new BankOpenAccountMessageEvent()));
    }

    public override void UpdateState(BoundUserInterfaceState state)
    {
        if (state is BankUiState bankState)
            _fragment?.UpdateState(bankState);
    }
}
