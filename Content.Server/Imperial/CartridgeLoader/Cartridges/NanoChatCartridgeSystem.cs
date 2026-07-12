using System.Linq;
using Content.Server.Power.Components;
using Content.Shared.Access.Components;
using Content.Shared.CartridgeLoader;
using Content.Shared.Imperial.CartridgeLoader.Cartridges;
using Content.Shared.PDA;
using Content.Shared.Power;
using Robust.Shared.Audio.Systems;

namespace Content.Server.Imperial.CartridgeLoader.Cartridges;

public sealed class NanoChatCartridgeSystem : EntitySystem
{
    [Dependency] private readonly SharedUserInterfaceSystem _userInterfaceSystem = null!;
    [Dependency] private readonly SharedAudioSystem _audio = null!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<NanoChatCartridgeComponent, CartridgeUiReadyEvent>(OnUiReady);
        SubscribeLocalEvent<NanoChatCartridgeComponent, CartridgeMessageEvent>(OnUiMessage);
        SubscribeLocalEvent<NanoChatCartridgeComponent, ComponentRemove>(OnCartridgeRemoved);

        SubscribeLocalEvent<NanoChatServerComponent, PowerChangedEvent>(OnPowerChanged);
    }

    private void OnPowerChanged(Entity<NanoChatServerComponent> ent, ref PowerChangedEvent args)
    {
        UpdateAllClients(ent);
    }

    private void OnCartridgeRemoved(Entity<NanoChatCartridgeComponent> ent, ref ComponentRemove args)
    {
        if (ent.Comp.ConnectedServer == null ||
            !TryComp<NanoChatServerComponent>(ent.Comp.ConnectedServer.Value, out var serverComp))
            return;

        serverComp.ConnectedClients.Remove(ent.Owner);
        UpdateAllClients((ent.Comp.ConnectedServer.Value, serverComp));
    }

    private void OnUiReady(Entity<NanoChatCartridgeComponent> ent, ref CartridgeUiReadyEvent args)
    {
        SyncUserToServer(ent);
        if (ent.Comp.SelectedChat != null)
            ent.Comp.UnreadMessages.Remove(ent.Comp.SelectedChat.Value);

        var server = GetServerForCartridge(ent.Owner);

        if (server != null)
            UpdateAllClients(server.Value);
        else
            UpdateUi(ent);
    }

    private void OnUiMessage(Entity<NanoChatCartridgeComponent> ent, ref CartridgeMessageEvent args)
    {
        switch (args)
        {
            case NanoChatSelectChatEvent select:
                ent.Comp.SelectedChat = select.ChatId;
                ent.Comp.UnreadMessages.Remove(select.ChatId);
                UpdateUi(ent);
                break;

            case NanoChatAddMembersEvent addMembers:
                HandleAddMembers(ent, addMembers);
                break;

            case NanoChatCreateChatEvent create:
                HandleCreateChat(ent, create.ChatName);
                break;

            case NanoChatUiActionEvent action:
                if (action.Action == NanoChatUiAction.NotificationSwitch)
                {
                    ent.Comp.NotificationsOn = !ent.Comp.NotificationsOn;
                    UpdateUi(ent);
                }
                break;

            case NanoChatSendEvent send:
                HandleSendText(ent, send.Text);
                break;
        }
    }

    private void HandleAddMembers(Entity<NanoChatCartridgeComponent> ent, NanoChatAddMembersEvent args)
    {
        var server = GetServerForCartridge(ent.Owner);
        if (server == null || ent.Comp.CurrentUserId == null)
            return;

        if (!server.Value.Comp.Chats.TryGetValue(args.ChatId, out var chat))
            return;

        if (!chat.Members.Contains(ent.Comp.CurrentUserId.Value))
            return;

        var addedAny = false;
        foreach (var newMember in args.AddedMembers.Where(newMember => !chat.Members.Contains(newMember)))
        {
            chat.Members.Add(newMember);
            addedAny = true;
        }

        if (!addedAny)
            return;

        Dirty(server.Value.Owner, server.Value.Comp);
        UpdateAllClients(server.Value);
    }

    private void HandleCreateChat(Entity<NanoChatCartridgeComponent> creator, string chatName)
    {
        if (string.IsNullOrWhiteSpace(chatName) || creator.Comp.CurrentUserId == null)
            return;

        var server = GetServerForCartridge(creator.Owner);
        if (server == null)
            return;

        var newChatId = server.Value.Comp.NextChatId++;
        var newChat = new NanoChatChat(newChatId, chatName, new List<NetEntity> { creator.Comp.CurrentUserId.Value });
        server.Value.Comp.Chats[newChatId] = newChat;
        creator.Comp.SelectedChat = newChatId;

        UpdateAllClients(server.Value);
    }

    private void HandleSendText(Entity<NanoChatCartridgeComponent> sender, string text)
    {
        if (string.IsNullOrWhiteSpace(text) || sender.Comp.SelectedChat == null || sender.Comp.CurrentUserId == null)
            return;

        var server = GetServerForCartridge(sender.Owner);
        if (server == null)
            return;

        var senderId = sender.Comp.CurrentUserId.Value;
        var chatId = sender.Comp.SelectedChat.Value;

        if (!server.Value.Comp.Chats.TryGetValue(chatId, out var chat))
            return;

        var senderName = sender.Comp.PdaCardName ?? Loc.GetString("nano-chat-ui-unknown-sender");
        var message = new NanoChatMessage(
            chatId,
            senderId,
            senderName,
            text
        );

        server.Value.Comp.Messages.Add(message);
        Dirty(server.Value.Owner, server.Value.Comp);

        foreach (var clientUid in server.Value.Comp.ConnectedClients)
        {
            if (!TryComp<NanoChatCartridgeComponent>(clientUid, out var targetComp)
                || targetComp.CurrentUserId == null)
                continue;

            if (!chat.Members.Contains(targetComp.CurrentUserId.Value))
                continue;

            if (targetComp.CurrentUserId == senderId)
                continue;

            if (targetComp.SelectedChat != chatId)
            {
                targetComp.UnreadMessages.TryAdd(chatId, 0);
                targetComp.UnreadMessages[chatId]++;
            }

            if (targetComp.NotificationsOn)
                _audio.PlayPvs(targetComp.NotificationSound, clientUid);
        }

        UpdateAllClients(server.Value);
    }

    private void SyncUserToServer(Entity<NanoChatCartridgeComponent> ent)
    {
        var loaderUid = Transform(ent).ParentUid;
        var pdaData = GetPdaData(loaderUid);

        if (pdaData.Id == null)
            return;

        ent.Comp.CurrentUserId = pdaData.Id.Value;
        ent.Comp.PdaCardName = pdaData.Name;

        var server = GetServerForCartridge(ent.Owner);
        if (server == null)
            return;

        if (ent.Comp.ConnectedServer != server.Value.Owner)
        {
            if (ent.Comp.ConnectedServer != null && TryComp<NanoChatServerComponent>(ent.Comp.ConnectedServer.Value, out var oldServer))
                oldServer.ConnectedClients.Remove(ent.Owner);
            ent.Comp.ConnectedServer = server.Value.Owner;
        }

        server.Value.Comp.ConnectedClients.Add(ent.Owner);

        var contact = new NanoChatContact(pdaData.Id.Value, pdaData.Name ?? Loc.GetString("nano-chat-ui-unknown-sender"), pdaData.Job, pdaData.Icon);
        server.Value.Comp.Users[pdaData.Id.Value] = contact;

        foreach (var otherId in server.Value.Comp.Users.Keys)
        {
            if (otherId == pdaData.Id.Value)
                continue;

            var chatExists = server.Value.Comp.Chats.Values.Any(c =>
                c.Members.Count == 2 && c.Members.Contains(pdaData.Id.Value) && c.Members.Contains(otherId));

            if (chatExists)
                continue;

            var newChatId = server.Value.Comp.NextChatId++;
            server.Value.Comp.Chats[newChatId] = new NanoChatChat(newChatId, "REMVEEASD", new List<NetEntity> { pdaData.Id.Value, otherId });
        }

        Dirty(server.Value.Owner, server.Value.Comp);
    }

    private void UpdateAllClients(Entity<NanoChatServerComponent> server)
    {
        server.Comp.ConnectedClients.RemoveWhere(uid => !Exists(uid));

        var clients = server.Comp.ConnectedClients.ToList();

        foreach (var clientUid in clients)
        {
            if (TryComp<NanoChatCartridgeComponent>(clientUid, out var comp))
                UpdateUi((clientUid, comp));
        }
    }

    private Entity<NanoChatServerComponent>? GetServerForCartridge(EntityUid cartridgeUid)
    {
        if (!TryComp(cartridgeUid, out TransformComponent? transform))
            return null;

        var serverQuery = EntityQueryEnumerator<NanoChatServerComponent, TransformComponent, ApcPowerReceiverComponent>();
        while (serverQuery.MoveNext(out var serverUid, out var serverComponent, out var serverTransform, out var power))
        {
            if (serverTransform.MapID == transform.MapID && power.Powered)
                return (serverUid, serverComponent);
        }
        return null;
    }

    private void UpdateUi(Entity<NanoChatCartridgeComponent> ent)
    {
        var comp = ent.Comp;
        var loaderUid = Transform(ent).ParentUid;

        if (!loaderUid.IsValid())
            return;

        SyncUserToServer(ent);

        var server = GetServerForCartridge(ent);
        var isServerOnline = server != null && TryComp<ApcPowerReceiverComponent>(server.Value.Owner, out var receiverComponent) && receiverComponent.Powered;

        var isContactReachable = false;
        var contacts = new List<NanoChatContact>();
        var chats = new List<NanoChatChat>();
        var history = new List<NanoChatMessage>();
        NanoChatChat? currentChat = null;

        if (isServerOnline && server != null && comp.CurrentUserId != null)
        {
            contacts = server.Value.Comp.Users.Values.ToList();

            chats = server.Value.Comp.Chats.Values
                .Where(c => c.Members.Contains(comp.CurrentUserId.Value))
                .OrderBy(c => c.Name)
                .ToList();

            if (comp.SelectedChat != null && server.Value.Comp.Chats.TryGetValue(comp.SelectedChat.Value, out currentChat))
            {
                history = server.Value.Comp.Messages
                    .Where(m => m.ChatId == comp.SelectedChat.Value)
                    .ToList();

                isContactReachable = server.Value.Comp.ConnectedClients.Any(uid =>
                    TryComp<NanoChatCartridgeComponent>(uid, out var c) &&
                    c.CurrentUserId != comp.CurrentUserId &&
                    c.CurrentUserId != null &&
                    currentChat.Members.Contains(c.CurrentUserId.Value));
            }
        }

        var state = new NanoChatBoundUserInterfaceState(
            comp.NotificationsOn,
            comp.CurrentUserId,
            currentChat,
            comp.PdaCardName,
            chats,
            contacts,
            history,
            isServerOnline,
            isContactReachable,
            comp.UnreadMessages
        );

        if (TryComp<CartridgeLoaderComponent>(loaderUid, out var loader) && _userInterfaceSystem.HasUi(loaderUid, loader.UiKey))
            _userInterfaceSystem.SetUiState(loaderUid, loader.UiKey, state);
    }

    private (NetEntity? Id, string? Name, string? Job, string? Icon) GetPdaData(EntityUid pdaUid)
    {
        if (!TryComp<PdaComponent>(pdaUid, out var pda))
            return (null, null, null, null);

        var name = pda.OwnerName;
        string? job = null;
        string? icon = null;
        NetEntity? id = null;

        if (!pda.ContainedId.HasValue || !TryComp<IdCardComponent>(pda.ContainedId.Value, out var idCard))
            return (id, name, job, icon);

        id = GetNetEntity(pda.ContainedId.Value);
        job = idCard.LocalizedJobTitle;
        icon = idCard.JobIcon;

        return (id, name, job, icon);
    }
}
