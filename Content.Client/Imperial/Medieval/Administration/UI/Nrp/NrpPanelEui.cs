using Content.Client.Administration;
using Content.Client.Eui;
using Content.Shared.Eui;
using Content.Shared.Imperial.Medieval.Administration.Nrp;

namespace Content.Client.Imperial.Medieval.Administration.UI.Nrp;

public sealed class NrpPanelEui : BaseEui
{

    [Dependency] private readonly ILogManager _logManager = default!;

    private ISawmill _sawmill;
    private Dictionary<NrpMessage, NrpMessageControl> _messageControls = new();
    private NrpStatsWindow _statsWindow;

    public NrpPanelEui()
    {
        IoCManager.InjectDependencies(this);
        _sawmill = _logManager.GetSawmill("nrp.panel");
        NrpPanel = new NrpPanel();
        NrpPanel.OnClose += OnCloseWindow;
        NrpPanel.StatsButton().OnPressed += _ => RequestStats();
        _statsWindow = new NrpStatsWindow();
    }

    private void RequestStats()
    {
        SendMessage(new NrpStatsRequest());
    }


    private NrpPanel? NrpPanel { get; set; }
    private void OnCloseWindow()
    {
        SendMessage(new CloseEuiMessage());
    }

    private void RequestMessages()
    {
        var request = new NrpMessagesRequest();
        SendMessage(request);
    }

    private void CreateMessageControl(NrpMessage message)
    {
        var messageControl = new NrpMessageControl(message.PlayerName, message.PlayerId, message.Message);
        messageControl.ResolveRpButton.OnPressed += _ => OnNrpResolve(message, false);
        messageControl.ResolveNrpButton.OnPressed += _ => OpenConfirmDialog(message);
        _messageControls.Add(message, messageControl);
        NrpPanel?.MessagesContainer.AddChild(messageControl);
    }

    public override void HandleMessage(EuiMessageBase msg)
    {
        base.HandleMessage(msg);
        switch (msg)
        {
            case NrpMessagesResponse response:
                NrpPanel?.MessagesContainer.DisposeAllChildren();
                foreach (var message in response.Messages)
                {
                    CreateMessageControl(message);
                }
                break;
            case NewNrpMessageMsg newMsg:
                CreateMessageControl(newMsg.Message);
                break;
            case RemoveNrpMessageMsg removeMsg:
                _messageControls[removeMsg.Message].Dispose();
                _messageControls.Remove(removeMsg.Message);
                break;
            case NrpStatsResponse response:
                var stats = response.Entries;
                _statsWindow.PopulateEntries(stats);
                _statsWindow.OpenCentered();
                break;
        }
    }

    private void OpenConfirmDialog(NrpMessage message)
    {
        var confirmDialog = new ConfirmDialog(() =>
            OnNrpResolve(message, true));
        confirmDialog.OpenCentered();

    }

    private void OnNrpResolve(NrpMessage message, bool isNrp)
    {
        SendMessage(new ResolveNrpMessageMsg(message, isNrp));
    }

    public override void Opened()
    {
        base.Opened();

        NrpPanel?.OpenCentered();
        RequestMessages();
    }

    public override void Closed()
    {
        base.Closed();

        NrpPanel?.Dispose();
    }
}
