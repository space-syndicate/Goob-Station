using Content.Client.Eui;
using Content.Shared._Aquila.Economy;
using Content.Shared.Eui;

namespace Content.Client._Aquila.Economy;

public sealed class BankAdminEui : BaseEui
{
    private readonly BankAdminWindow _window;

    public BankAdminEui()
    {
        _window = new BankAdminWindow();
        _window.OnClose += () => SendMessage(new CloseEuiMessage());
        _window.OnSelect += number => SendMessage(new BankAdminSelectMessage(number));
        _window.OnRefresh += () => SendMessage(new BankAdminRefreshMessage());
        _window.OnBonus += (number, amount, comment) => SendMessage(new BankAdminBonusMessage(number, amount, comment));
        _window.OnFine += (number, amount, comment) => SendMessage(new BankAdminFineMessage(number, amount, comment));
        _window.OnSetBalance += (number, balance, comment) => SendMessage(new BankAdminSetBalanceMessage(number, balance, comment));
        _window.OnRevert += (number, index, amount, comment) => SendMessage(new BankAdminRevertMessage(number, index, amount, comment));
        _window.OnRequestReply += (id, approve) => SendMessage(new BankAdminRequestReplyMessage(id, approve));
        _window.OnRevokeFine += id => SendMessage(new BankAdminRevokeFineMessage(id));
    }

    public override void Opened()
    {
        _window.OpenCentered();
    }

    public override void Closed()
    {
        _window.Close();
    }

    public override void HandleState(EuiStateBase state)
    {
        if (state is BankAdminEuiState bankState)
            _window.UpdateState(bankState);
    }
}
