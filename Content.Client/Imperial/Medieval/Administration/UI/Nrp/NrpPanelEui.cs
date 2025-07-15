using System.Linq;
using System.Security.Principal;
using Content.Client.Eui;
using Content.Shared.Eui;
using Content.Shared.Imperial.Medieval.Administration.Nrp;

namespace Content.Client.Imperial.Medieval.Administration.UI.Nrp;

public sealed class NrpPanelEui : BaseEui
{

    #region Internal classes
    internal class MultiMap<TKey, TValue> where TKey : notnull
    {
        private readonly Dictionary<TKey, List<TValue>> _map = new();

        public void Add(TKey key, TValue value)
        {
            if (!_map.TryGetValue(key, out var list))
            {
                list = new List<TValue>();
                _map[key] = list;
            }

            list.Add(value);
        }

        public bool Remove(TKey key)
        {
            return _map.Remove(key);
        }

        public bool Remove(TKey key, TValue value)
        {
            if (!_map.TryGetValue(key, out var list))
                return false;

            var removed = list.Remove(value);

            if (list.Count == 0)
                _map.Remove(key);

            return removed;

        }

        public void ForEach(TKey key, Action<TValue> action)
        {
            if (!_map.TryGetValue(key, out var list))
                return;

            foreach (var item in list)
            {
                action(item);
            }
        }

        public IEnumerable<TValue> this[TKey key]
        {
            get
            {
                if (_map.TryGetValue(key, out var list))
                    return list;
                return Enumerable.Empty<TValue>();
            }
        }
    }
    #endregion

    [Dependency] private readonly ILogManager _logManager = default!;

    private ISawmill _sawmill;
    private Dictionary<NrpMessage, NrpMessageControl> _messageControls = new();
    private NrpStatsWindow _statsWindow;
    private MultiMap<NrpMessage, ConfirmDialog> _dialogs = new();

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
        var messageControl = new NrpMessageControl(message.PlayerName, message.EntityName, message.JobName, message.PlayerId, message.Message);
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
                _dialogs.ForEach(removeMsg.Message, d => d.Dispose());
                _dialogs.Remove(removeMsg.Message);
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
        var confirmDialog = new ConfirmDialog(message);
        confirmDialog.OnConfirmButtonPressed(() => OnNrpResolve(confirmDialog, true));
        confirmDialog.OpenCentered();
        _dialogs.Add(message, confirmDialog);

    }

    private void OnNrpResolve(ConfirmDialog dialog, bool isNrp)
    {
        _dialogs.ForEach(dialog.Message, d => d.Dispose());
        _dialogs.Remove(dialog.Message);
        OnNrpResolve(dialog.Message, isNrp);
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
