using Content.Server.Administration.Managers;
using Content.Server.EUI;
using Content.Shared.Administration;
using Content.Shared.Administration.Logs;
using Content.Shared.Eui;
using Content.Server.Administration;

namespace Content.Server.Imperial.Medieval.Administration.Nrp;

public sealed class NrpPanelEui : BaseEui
{
    [Dependency] private readonly IAdminManager _adminManager = default!;


    public NrpPanelEui()
    {
        IoCManager.InjectDependencies(this);
    }
    public override void Opened()
    {
        base.Opened();

        _adminManager.OnPermsChanged += OnPermsChanged;
    }

    private void OnPermsChanged(AdminPermsChangedEventArgs args)
    {
        if (args.Player == Player && !_adminManager.HasAdminFlag(Player, AdminFlags.Logs))
        {
            Close();
        }
    }

    public override EuiStateBase GetNewState()
    {
        // TODO
        // var state = new State(...);
        // return state;

        // placeholder
        var state = new AdminLogsEuiState(1, new Dictionary<Guid, string>(), 0);
        return state;
    }

    public override void HandleMessage(EuiMessageBase msg)
    {
        base.HandleMessage(msg);

        if (!_adminManager.HasAdminFlag(Player, AdminFlags.Logs))
        {
            return;
        }

        switch (msg)
        {

        }
    }

    private void SendNrp(bool replace)
    {
        // TODO
        // SendMessage(message);
    }

    public override void Closed()
    {
        base.Closed();

        _adminManager.OnPermsChanged -= OnPermsChanged;
    }
}
