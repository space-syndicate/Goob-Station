using Content.Shared.CartridgeLoader;
using Content.Shared.PDA;
using Robust.Shared.Network;

namespace Content.Shared.Imperial.CartridgeLoader.Cartridges;

public sealed class NanoChatCartridgeSystem : EntitySystem
{
    [Dependency] private readonly SharedUserInterfaceSystem _userInterfaceSystem = null!;
    [Dependency] private readonly INetManager _net = null!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NanoChatCartridgeComponent, CartridgeUiReadyEvent>(OnUiReady);
        SubscribeLocalEvent<NanoChatCartridgeComponent, CartridgeMessageEvent>(OnUiMessage);
    }

    private void OnUiReady(Entity<NanoChatCartridgeComponent> ent, ref CartridgeUiReadyEvent args)
    {
        UpdateUi(ent);
    }

    private void OnUiMessage(Entity<NanoChatCartridgeComponent> ent, ref CartridgeMessageEvent args)
    {
        switch (args)
        {
            case NanoChatSelectContactMessage select:
                ent.Comp.SelectedContact = select.ContactName;
                break;

            case NanoChatUiActionMessage action:
                if (action.Action == NanoChatUiAction.NotificationSwitch)
                    ent.Comp.NotificationsOn = !ent.Comp.NotificationsOn;
                break;

            case NanoChatSendTextMessage send:
                HandleSendText(ent, send.Text);
                break;
        }

        UpdateUi(ent);
    }

    private void HandleSendText(Entity<NanoChatCartridgeComponent> sender, string text)
    {
        var comp = sender.Comp;

        if (string.IsNullOrWhiteSpace(text) || string.IsNullOrEmpty(comp.SelectedContact))
            return;

        EnsurePdaName(sender);
        var senderName = comp.PdaCardName ?? Loc.GetString("nano-chat-ui-unknown-sender");
        var targetName = comp.SelectedContact;

        AddMessageToHistory(sender, targetName, senderName, text);

        if (!_net.IsServer)
            return;

        var query = EntityQueryEnumerator<NanoChatCartridgeComponent>();
        while (query.MoveNext(out var uid, out var targetComp))
        {
            if (uid == sender.Owner)
                continue;

            EnsurePdaName((uid, targetComp));

            if (targetComp.PdaCardName != targetName)
                continue;

            AddMessageToHistory((uid, targetComp), senderName, senderName, text);

            UpdateUi((uid, targetComp));
            break;
        }
    }

    private void AddMessageToHistory(Entity<NanoChatCartridgeComponent> ent, string contactKey, string messageSender, string text)
    {
        if (!ent.Comp.ChatHistories.TryGetValue(contactKey, out var history))
        {
            history = new List<NanoChatMessage>();
            ent.Comp.ChatHistories[contactKey] = history;
        }

        history.Add(new NanoChatMessage(messageSender, text));
        Dirty(ent);
    }

    private void EnsurePdaName(Entity<NanoChatCartridgeComponent> ent)
    {
        var loaderUid = Transform(ent).ParentUid;

        if (TryComp<PdaComponent>(loaderUid, out var pdaComp) && !string.IsNullOrEmpty(pdaComp.OwnerName))
            ent.Comp.PdaCardName = pdaComp.OwnerName;

    }

    private List<string> GetContacts(Entity<NanoChatCartridgeComponent> currentEnt)
    {
        var contacts = new HashSet<string>();

        if (_net.IsServer)
        {
            var query = EntityQueryEnumerator<NanoChatCartridgeComponent>();
            while (query.MoveNext(out var uid, out var comp))
            {
                if (uid == currentEnt.Owner)
                    continue;

                EnsurePdaName((uid, comp));
                if (!string.IsNullOrEmpty(comp.PdaCardName))
                    contacts.Add(comp.PdaCardName);
            }
        }

        foreach (var historicContact in currentEnt.Comp.ChatHistories.Keys)
            contacts.Add(historicContact);

        var list = new List<string>(contacts);
        list.Sort();
        return list;
    }

    private void UpdateUi(Entity<NanoChatCartridgeComponent> ent)
    {
        var comp = ent.Comp;
        var loaderUid = Transform(ent).ParentUid;

        if (!loaderUid.IsValid())
            return;

        EnsurePdaName(ent);

        var history = comp.SelectedContact != null && comp.ChatHistories.TryGetValue(comp.SelectedContact, out var chatHistory)
            ? chatHistory
            : new List<NanoChatMessage>();

        var state = new NanoChatBoundUserInterfaceState(
            comp.NotificationsOn,
            comp.SelectedContact,
            comp.PdaCardName,
            GetContacts(ent),
            history
        );

        if (!TryComp<CartridgeLoaderComponent>(loaderUid, out var loader))
            return;

        if (_userInterfaceSystem.HasUi(loaderUid, loader.UiKey))
            _userInterfaceSystem.SetUiState(loaderUid, loader.UiKey, state);
    }
}
