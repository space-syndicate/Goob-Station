using Content.Server._Aquila.Economy.Systems;
using Content.Server.EUI;
using Content.Shared._Aquila.Economy;
using Content.Shared.Eui;

namespace Content.Server._Aquila.Economy;

public sealed class BankAdminEui : BaseEui
{
    [Dependency] private readonly IEntitySystemManager _systems = default!;

    private int? _selected;

    public BankAdminEui(int? selected)
    {
        IoCManager.InjectDependencies(this);
        _selected = selected;
    }

    public override void Opened()
    {
        StateDirty();
    }

    public override EuiStateBase GetNewState()
    {
        return _systems.GetEntitySystem<BankAdminSystem>().GetPanelState(_selected);
    }

    public override void HandleMessage(EuiMessageBase msg)
    {
        base.HandleMessage(msg);

        var bank = _systems.GetEntitySystem<BankAdminSystem>();
        if (!bank.CanManage(Player))
        {
            Close();
            return;
        }

        switch (msg)
        {
            case BankAdminSelectMessage select:
                _selected = select.Number;
                break;
            case BankAdminRefreshMessage:
                break;
            case BankAdminBonusMessage bonus:
                bank.Bonus(Player, bonus.Number, bonus.Amount, bonus.Comment);
                break;
            case BankAdminFineMessage fine:
                bank.Fine(Player, fine.Number, fine.Amount, fine.Comment);
                break;
            case BankAdminSetBalanceMessage balance:
                bank.SetBalance(Player, balance.Number, balance.Balance, balance.Comment);
                break;
            case BankAdminRevertMessage revert:
                bank.Revert(Player, revert.Number, revert.Index, revert.Amount, revert.Comment);
                break;
            case BankAdminRequestReplyMessage reply:
                bank.ResolveRequest(Player, reply.Id, reply.Approve);
                break;
            case BankAdminRevokeFineMessage revoke:
                bank.RevokeFine(Player, revoke.Id);
                break;
            default:
                return;
        }

        StateDirty();
    }
}
