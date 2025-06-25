using Content.Client.Eui;
using Content.Shared.Eui;
namespace Content.Client.Imperial.Medieval.Administration.UI.Nrp;

public sealed class NrpPanelEui : BaseEui
{
    public NrpPanelEui()
    {
        NrpPanel = new NrpPanel();
        NrpPanel.OnClose += OnCloseWindow;
    }


    private NrpPanel? NrpPanel { get; set; }
    private bool FirstState { get; set; } = true;

    private void OnCloseWindow()
    {
        SendMessage(new CloseEuiMessage());
    }

    private void RequestMessages()
    {
        // TODO
        // var request = new ...
        // SendMessage(request);
    }

    public override void HandleState(EuiStateBase state)
    {
        // TODO
        // var s = (NrpPanelState) state;

        if(!FirstState)
            return;

        FirstState = false;
        RequestMessages();
    }

    public override void HandleMessage(EuiMessageBase msg)
    {
        base.HandleMessage(msg);

        switch (msg)
        {
            // TODO
        }
    }

    public override void Opened()
    {
        base.Opened();

        NrpPanel?.OpenCentered();
    }

    public override void Closed()
    {
        base.Closed();

        NrpPanel?.Dispose();
    }
}
