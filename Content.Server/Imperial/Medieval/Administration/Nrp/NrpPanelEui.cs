using Content.Server.Administration.Managers;
using Content.Server.EUI;
using Content.Shared.Administration;
using Content.Shared.Eui;
using Content.Server.Administration;
using Content.Shared.Imperial.Medieval.Administration.Nrp;
using Microsoft.Extensions.Logging;

namespace Content.Server.Imperial.Medieval.Administration.Nrp;

public sealed class NrpPanelEui : BaseEui
{
    [Dependency] private readonly IEntitySystemManager _entitySystemManager = default!;
    [Dependency] private readonly IAdminManager _adminManager = default!;
    private readonly NrpMessagesSystem _nrpSystem;
    private readonly ISawmill _sawmill;

    public NrpPanelEui()
    {
        IoCManager.InjectDependencies(this);
        _sawmill = Logger.GetSawmill("NrpPanelEui");
        _nrpSystem = _entitySystemManager.GetEntitySystem<NrpMessagesSystem>();
    }
    public override void Opened()
    {
        base.Opened();
        _nrpSystem.RegisterEui(this);
        _adminManager.OnPermsChanged += OnPermsChanged;
    }

    public override void Closed()
    {
        base.Closed();

        _nrpSystem.UnregisterEui(this);
        _adminManager.OnPermsChanged -= OnPermsChanged;
    }


    private void OnPermsChanged(AdminPermsChangedEventArgs args)
    {
        if (args.Player == Player && !_adminManager.HasAdminFlag(Player, AdminFlags.Logs))
        {
            Close();
        }
    }

    public void SendNewMessage(NrpMessage message)
    {
        SendMessage(new NewNrpMessageMsg(message));
    }

    public override async void HandleMessage(EuiMessageBase msg)
    {
        base.HandleMessage(msg);

        if (!_adminManager.HasAdminFlag(Player, AdminFlags.Logs))
            return;


        switch (msg)
        {
            case NrpMessagesRequest:
                var messages = _nrpSystem.GetAllMessages();
                SendMessage(new NrpMessagesResponse(messages));
                break;
            case ResolveNrpMessageMsg resolve:
                var isNrp = resolve.IsNrp;
                if (isNrp)
                {
                    await _nrpSystem.AddPlayerNrpViolation(resolve.Message.PlayerId);
                    var violations = await _nrpSystem.GetPlayerNrpViolations(resolve.Message.PlayerId, 3);
                    _sawmill.Debug($"{resolve.Message.PlayerName}: {violations} nrp messages");
                    _nrpSystem.OnViolation(resolve.Message, violations, Player.UserId);
                }
                _nrpSystem.RemoveMessage(resolve.Message);
                _nrpSystem.AddResolveToStats(Player.Name);
                SendMessage(new RemoveNrpMessageMsg(resolve.Message));
                break;
            case NrpStatsRequest:
                var stats = _nrpSystem.GetStats();
                SendMessage(new NrpStatsResponse(stats));
                break;
        }
    }


}
