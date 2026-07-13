using System.Linq;
using Content.Server.GameTicking;
using Content.Server.Power.Components;
using Content.Shared.Access.Components;
using Content.Shared.CartridgeLoader;
using Content.Shared.GPS.Components;
using Content.Shared.Imperial.CartridgeLoader.Cartridges;
using Content.Shared.PDA;
using Content.Shared.Power;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Timing;

namespace Content.Server.Imperial.CartridgeLoader.Cartridges;

public sealed class NanoChatCartridgeSystem : EntitySystem
{
    [Dependency] private readonly SharedUserInterfaceSystem _userInterfaceSystem = null!;
    [Dependency] private readonly SharedAudioSystem _audio = null!;
    [Dependency] private readonly TransformSystem _transform = null!;
    [Dependency] private readonly GameTicker _gameTicker = null!;
    [Dependency] private readonly IGameTiming _timing = null!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<NanoChatCartridgeComponent, CartridgeUiReadyEvent>(OnUiReady);
        SubscribeLocalEvent<NanoChatCartridgeComponent, CartridgeMessageEvent>(OnUiMessage);
        SubscribeLocalEvent<NanoChatCartridgeComponent, ComponentRemove>(OnCartridgeRemoved);

        SubscribeLocalEvent<NanoChatServerComponent, PowerChangedEvent>(OnPowerChanged);

        SubscribeLocalEvent<NanoChatServerComponent, NanoChatServerStartupEvent>(OnServerStartup);
        SubscribeLocalEvent<NanoChatServerComponent, NanoChatServerShutdownEvent>(OnServerShutdown);
    }

    private void OnPowerChanged(Entity<NanoChatServerComponent> ent, ref PowerChangedEvent args)
    {
        UpdateAllClients(ent);
    }

    private void OnServerShutdown(Entity<NanoChatServerComponent> ent, ref NanoChatServerShutdownEvent args)
    {
        UpdateAllClients(ent);
    }

    private void OnServerStartup(Entity<NanoChatServerComponent> ent, ref NanoChatServerStartupEvent args)
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
                switch (action.Action)
                {
                    case NanoChatUiAction.NotificationSwitch:
                        ent.Comp.NotificationsOn = !ent.Comp.NotificationsOn;
                        UpdateUi(ent);
                        break;
                    case NanoChatUiAction.SendLocation:
                        var parent = Transform(ent).ParentUid;
                        if (!HasComp<HandheldGPSComponent>(parent))
                            return;

                        var posText = Loc.GetString("nano-chat-ui-chat-windows-message-error");
                        var pos = _transform.GetMapCoordinates(ent);

                        if (pos.MapId != MapId.Nullspace)
                        {
                            var x = (int) pos.Position.X;
                            var y = (int) pos.Position.Y;
                            posText = $"({x}, {y})";
                        }

                        HandleSendText(ent, Loc.GetString("handheld-gps-coordinates-title", ("coordinates", posText)));
                        break;
                    default:
                        return;
                }
                break;

            case NanoChatSendEvent send:
                HandleSendText(ent, send.Text);
                break;

            case NanoChatTypingEvent:
                HandleTyping(ent);
                break;
        }
    }

    private void HandleTyping(Entity<NanoChatCartridgeComponent> sender)
    {
        if (sender.Comp.SelectedChat == null || sender.Comp.UserId == null)
            return;

        var server = GetServerForCartridge(sender.Owner);
        if (server == null)
            return;

        var chatId = sender.Comp.SelectedChat.Value;
        var userId = sender.Comp.UserId.Value;

        if (!server.Value.Comp.TypingTimeouts.ContainsKey(chatId))
            server.Value.Comp.TypingTimeouts[chatId] = new Dictionary<NetEntity, TimeSpan>();

        server.Value.Comp.TypingTimeouts[chatId][userId] = _timing.CurTime + server.Value.Comp.TypingTimeout;

        Dirty(server.Value.Owner, server.Value.Comp);
        UpdateAllClients(server.Value);
    }

    private void HandleAddMembers(Entity<NanoChatCartridgeComponent> ent, NanoChatAddMembersEvent args)
    {
        var server = GetServerForCartridge(ent.Owner);
        if (server == null || ent.Comp.UserId == null)
            return;

        var chat = server.Value.Comp.Chats.FirstOrDefault(c => c.Id == args.ChatId);
        if (chat == null)
            return;

        if (!chat.Members.Contains(ent.Comp.UserId.Value))
            return;

        var addedAny = false;
        foreach (var newMember in args.AddedMembers.Where(newMember => !chat.Members.Contains(newMember)))
        {
            chat.Members.Add(newMember);
            addedAny = true;
        }

        if (!addedAny)
            return;

        if (string.IsNullOrEmpty(chat.Name))
        {
            var memberNames = chat.Members
                .Select(id => server.Value.Comp.Users.TryGetValue(id, out var user)
                    ? user.Name
                    : Loc.GetString("nano-chat-ui-chat-window-sender-unknown"))
                .ToList();

            chat.Name = string.Join(", ", memberNames);
        }

        Dirty(server.Value.Owner, server.Value.Comp);
        UpdateAllClients(server.Value);
    }

    private void HandleCreateChat(Entity<NanoChatCartridgeComponent> creator, string chatName)
    {
        if (string.IsNullOrWhiteSpace(chatName) || creator.Comp.UserId == null)
            return;

        var server = GetServerForCartridge(creator.Owner);
        if (server == null)
            return;

        var newChatId = server.Value.Comp.NextChatId++;
        var newChat = new NanoChatChat(newChatId, chatName, creator.Comp.UserId.Value, new List<NetEntity> { creator.Comp.UserId.Value }, new List<NanoChatMessage>(), false);
        server.Value.Comp.Chats.Add(newChat);
        creator.Comp.SelectedChat = newChatId;

        UpdateAllClients(server.Value);
    }

    private void HandleSendText(Entity<NanoChatCartridgeComponent> sender, string text)
    {
        if (string.IsNullOrWhiteSpace(text) || sender.Comp.SelectedChat == null || sender.Comp.UserId == null)
            return;

        var server = GetServerForCartridge(sender.Owner);
        if (server == null)
            return;

        var senderId = sender.Comp.UserId.Value;
        var chatId = sender.Comp.SelectedChat.Value;

        var chat = server.Value.Comp.Chats.FirstOrDefault(c => c.Id == chatId);
        if (chat == null)
            return;

        if (server.Value.Comp.TypingTimeouts.TryGetValue(chatId, out var chatTyping))
            chatTyping.Remove(senderId);

        var senderName = sender.Comp.PdaCardName ?? Loc.GetString("nano-chat-ui-chat-window-sender-unknown");
        var message = new NanoChatMessage(
            senderId,
            senderName,
            text,
            _gameTicker.RoundDuration()
        );

        chat.Messages.Add(message);
        Dirty(server.Value.Owner, server.Value.Comp);

        foreach (var clientUid in server.Value.Comp.ConnectedClients)
        {
            if (!TryComp<NanoChatCartridgeComponent>(clientUid, out var targetComp)
                || targetComp.UserId == null)
                continue;

            if (!chat.Members.Contains(targetComp.UserId.Value))
                continue;

            if (targetComp.UserId == senderId)
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

        ent.Comp.UserId = pdaData.Id.Value;
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

        var contact = new NanoChatContact(pdaData.Id.Value, pdaData.Name ?? Loc.GetString("nano-chat-ui-chat-window-sender-unknown"), pdaData.Job, pdaData.Icon);
        server.Value.Comp.Users[pdaData.Id.Value] = contact;

        foreach (var otherId in server.Value.Comp.Users.Keys)
        {
            if (otherId == pdaData.Id.Value)
                continue;

            var chatExists = server.Value.Comp.Chats.Any(c =>
                c.Members.Count == 2 && c.Members.Contains(pdaData.Id.Value) && c.Members.Contains(otherId));

            if (chatExists)
                continue;

            var newChat = new NanoChatChat(server.Value.Comp.NextChatId++, string.Empty, null, new List<NetEntity> { pdaData.Id.Value, otherId }, new List<NanoChatMessage>());
            server.Value.Comp.Chats.Add(newChat);
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
        var cartTrans = Transform(cartridgeUid);

        var serverQuery = EntityQueryEnumerator<NanoChatServerComponent, TransformComponent, ApcPowerReceiverComponent>();
        while (serverQuery.MoveNext(out var serverUid, out var serverComponent, out var serverTransform, out var power))
        {
            if (serverTransform.MapID == cartTrans.MapID && power.Powered)
                return (serverUid, serverComponent);
        }
        return null;
    }

    private void UpdateUi(Entity<NanoChatCartridgeComponent> ent)
    {
        var comp = ent.Comp;
        var loaderUid = Transform(ent).ParentUid;

        var typingUsers = new Dictionary<NetEntity, string>();
        var curTime = _timing.CurTime;

        if (!loaderUid.IsValid())
            return;

        SyncUserToServer(ent);

        var server = GetServerForCartridge(ent);
        var isServerOnline = server != null && TryComp<ApcPowerReceiverComponent>(server.Value.Owner, out var receiverComponent) && receiverComponent.Powered;

        var isContactReachable = false;
        var contacts = new List<NanoChatContact>();
        var chats = new List<NanoChatChat>();
        NanoChatChat? currentChat = null;
        var canSendLocation = HasComp<HandheldGPSComponent>(loaderUid);

        if (isServerOnline && server != null && comp.UserId != null)
        {
            contacts = server.Value.Comp.Users.Values.ToList();

            chats = server.Value.Comp.Chats
                .Where(c => c.Members.Contains(comp.UserId.Value))
                .OrderBy(c => c.Name)
                .ToList();

            if (comp.SelectedChat != null)
            {
                currentChat = server.Value.Comp.Chats.FirstOrDefault(c => c.Id == comp.SelectedChat.Value);

                if (currentChat != null)
                {
                    isContactReachable = server.Value.Comp.ConnectedClients.Any(uid =>
                        TryComp<NanoChatCartridgeComponent>(uid, out var c) &&
                        c.UserId != comp.UserId &&
                        c.UserId != null &&
                        currentChat.Members.Contains(c.UserId.Value));
                }
            }
        }

        if (currentChat != null && server != null && server.Value.Comp.TypingTimeouts.TryGetValue(currentChat.Id, out var chatTyping))
        {
            var expiredUsers = new List<NetEntity>();

            foreach (var (typistId, expiration) in chatTyping)
            {
                if (curTime > expiration)
                {
                    expiredUsers.Add(typistId);
                    continue;
                }

                if (typistId == comp.UserId)
                    continue;

                if (server.Value.Comp.Users.TryGetValue(typistId, out var contact))
                    typingUsers[typistId] = contact.Name;
            }

            foreach (var expired in expiredUsers)
                chatTyping.Remove(expired);
        }

        var state = new NanoChatBoundUserInterfaceState(
            comp.NotificationsOn,
            comp.UserId,
            currentChat,
            chats,
            contacts,
            isServerOnline,
            isContactReachable,
            canSendLocation,
            comp.UnreadMessages,
            typingUsers
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
