using Content.Server.Administration.Logs;
using Content.Server.CartridgeLoader;
using Content.Server.GameTicking;
using Content.Server.Power.Components;
using Content.Shared.Access.Components;
using Content.Shared.CartridgeLoader;
using Content.Shared.Database;
using Content.Shared.GPS.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Imperial.CartridgeLoader.Cartridges;
using Content.Shared.PDA;
using Content.Shared.Paper;
using Content.Shared.Power;
using Content.Shared.StatusIcon;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using System.Linq;
using Content.Shared.Whitelist;

namespace Content.Server.Imperial.CartridgeLoader.Cartridges;

/// <summary>
/// Система, отвечающая за работу функционала картриджей НаноЧата.
/// Управляет связью с серверами, обработкой сообщений, синхронизацией пользователей и интерфейсом.
/// </summary>
/// <seealso cref="NanoChatCartridgeComponent"/>
/// <seealso cref="NanoChatServerComponent"/>
public sealed class NanoChatCartridgeSystem : EntitySystem
{
    [Dependency] private readonly CartridgeLoaderSystem _loaderSystem = null!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = null!;
    [Dependency] private readonly GameTicker _gameTicker = null!;
    [Dependency] private readonly IAdminLogManager _adminLogger = null!;
    [Dependency] private readonly IComponentFactory _componentFactory = null!;
    [Dependency] private readonly IGameTiming _timing = null!;
    [Dependency] private readonly IPrototypeManager _prototype = null!;
    [Dependency] private readonly MetaDataSystem _metaDataSystem = null!;
    [Dependency] private readonly PaperSystem _paper = null!;
    [Dependency] private readonly SharedAudioSystem _audio = null!;
    [Dependency] private readonly SharedHandsSystem _hands = null!;
    [Dependency] private readonly SharedUserInterfaceSystem _userInterfaceSystem = null!;
    [Dependency] private readonly TransformSystem _transform = null!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NanoChatCartridgeComponent, CartridgeMessageEvent>(OnUiMessage);
        SubscribeLocalEvent<NanoChatCartridgeComponent, CartridgeUiReadyEvent>(OnUiReady);

        SubscribeLocalEvent<NanoChatCartridgeComponent, CartridgeAddedEvent>(OnCartridgeAdded);
        SubscribeLocalEvent<NanoChatCartridgeComponent, ComponentRemove>(OnComponentCartridgeRemoved);

        SubscribeLocalEvent<NanoChatServerComponent, PowerChangedEvent>(OnPowerChanged);

        SubscribeLocalEvent<NanoChatServerComponent, NanoChatServerStartupEvent>(OnServerStartup);
        SubscribeLocalEvent<NanoChatServerComponent, NanoChatServerShutdownEvent>(OnServerShutdown);
    }

    private void OnUiMessage(Entity<NanoChatCartridgeComponent> ent, ref CartridgeMessageEvent args)
    {
        switch (args)
        {
            case NanoChatCreateChatEvent create:
                HandleCreateChat(ent, create.ChatName, create.Actor);
                break;
            case NanoChatEditChatEvent edit:
                HandleEditChat(ent, edit);
                break;

            case NanoChatAddMembersEvent addMembers:
                HandleAddMembers(ent, addMembers);
                break;
            case NanoChatRemoveMembersEvent removeMembers:
                HandleRemoveMembers(ent, removeMembers);
                break;

            case NanoChatSelectChatEvent select:
                ent.Comp.SelectedChat = select.ChatId;
                ent.Comp.UnreadMessages.Remove(select.ChatId);
                UpdateUi(ent);
                break;

            case NanoChatSendEvent send:
                HandleSendText(ent, send.Text, args.Actor);
                break;
            case NanoChatTypingEvent:
                HandleTyping(ent);
                break;
            case NanoChatPrintEvent print:
                HandlePrintChat(ent, print);
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

                        HandleSendText(ent, Loc.GetString("handheld-gps-coordinates-title", ("coordinates", posText)), args.Actor);
                        break;
                    default:
                        return;
                }
                break;
        }
    }

    private void OnUiReady(Entity<NanoChatCartridgeComponent> ent, ref CartridgeUiReadyEvent args)
    {
        SyncUserToServer(ent, out var server);
        if (ent.Comp.SelectedChat != null)
            ent.Comp.UnreadMessages.Remove(ent.Comp.SelectedChat.Value);


        if (server != null)
            UpdateAllClients(server.Value);
        else
            UpdateUi(ent);
    }

    private void OnCartridgeAdded(Entity<NanoChatCartridgeComponent> ent, ref CartridgeAddedEvent args)
    {
        SyncUserToServer(ent, out var server);
        if (server != null)
            UpdateAllClients(server.Value);
    }

    private void OnComponentCartridgeRemoved(Entity<NanoChatCartridgeComponent> ent, ref ComponentRemove args)
    {
        if (ent.Comp.ConnectedServer is not { } serverUid ||
            !TryComp(serverUid, out NanoChatServerComponent? server))
            return;

        UpdateAllClients((serverUid, server));
    }

    private void OnPowerChanged(Entity<NanoChatServerComponent> ent, ref PowerChangedEvent args)
    {
        UpdateAllClients(ent);
    }

    private void OnServerStartup(Entity<NanoChatServerComponent> ent, ref NanoChatServerStartupEvent args)
    {
        UpdateAllClients(ent);
    }

    private void OnServerShutdown(Entity<NanoChatServerComponent> ent, ref NanoChatServerShutdownEvent args)
    {
        UpdateAllClients(ent);
    }


    /// <summary>
    /// Обновляет UI картриджа для клиента.
    /// </summary>
    private void UpdateUi(Entity<NanoChatCartridgeComponent> ent, Entity<NanoChatServerComponent>? server = null)
    {
        var comp = ent.Comp;
        var loaderUid = Transform(ent).ParentUid;

        var typingUsers = new Dictionary<NetEntity, string>();
        var curTime = _timing.CurTime;

        if (!loaderUid.IsValid())
            return;

        if (server == null)
            SyncUserToServer(ent, out server);

        var isServerOnline =
            server != null &&
            TryComp<ApcPowerReceiverComponent>(server.Value, out var receiverComponent) &&
            receiverComponent.Powered;

        var isContactReachable = false;
        var contacts = new List<NanoChatContact>();
        var chats = new List<NanoChatChat>();
        NanoChatChat? currentChat = null;
        var canSendLocation = false;

        if (isServerOnline && server != null && comp.UserId != null)
        {
            contacts = ent.Comp.Visible
                ? server.Value.Comp.Users.Where(contact => contact.Visible).ToList()
                : server.Value.Comp.Users;

            if (ent.Comp.Visible)
            {
                chats = server.Value.Comp.Chats
                    .Where(c => c.Members.Contains(comp.UserId.Value))
                    .OrderBy(c => c.Name)
                    .ToList();
            }
            else
                chats = server.Value.Comp.Chats.OrderBy(c => c.Name).ToList();

            currentChat = server.Value.Comp.Chats.FirstOrDefault(c =>
                comp.SelectedChat != null &&
                c.Id == comp.SelectedChat.Value);

            if (comp.SelectedChat != null && currentChat != null)
            {
                canSendLocation = HasComp<HandheldGPSComponent>(loaderUid);

                var query = EntityQueryEnumerator<NanoChatCartridgeComponent>();

                while (query.MoveNext(out _, out var other))
                {
                    if (other.ConnectedServer != server.Value.Owner)
                        continue;

                    if (other.UserId == null || other.UserId == comp.UserId)
                        continue;

                    if (!currentChat.Members.Contains(other.UserId.Value))
                        continue;

                    isContactReachable = true;
                    break;
                }
            }
        }

        if (currentChat != null &&
            server != null &&
            server.Value.Comp.TypingTimeouts.TryGetValue(currentChat.Id, out var chatTyping))
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

                if (server.Value.Comp.Users.All(user => user.Id != typistId))
                    continue;

                var contact = server.Value.Comp.Users.First(user => user.Id == typistId);
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
            _timing.CurTime >= comp.NextPrintAllowedAfter,
            !ent.Comp.Visible,
            comp.UnreadMessages,
            typingUsers
        );

        if (TryComp<CartridgeLoaderComponent>(loaderUid, out var loader) &&
            _userInterfaceSystem.IsUiOpen(loaderUid, loader.UiKey))
        {
            _userInterfaceSystem.SetUiState(loaderUid, loader.UiKey, state);
        }
    }


    /// <summary>
    /// Создает новый чат на сервере.
    /// </summary>
    private void HandleCreateChat(Entity<NanoChatCartridgeComponent> creator, string chatName, EntityUid actor, List<NetEntity>? initialMembers = null)
    {
        if (string.IsNullOrWhiteSpace(chatName) || creator.Comp.UserId == null)
            return;

        var server = GetServerForCartridge(creator);
        if (server == null)
            return;

        var members = initialMembers ?? [creator.Comp.UserId.Value];

        var newChatId = server.Value.Comp.NextChatId++;
        var newChat = new NanoChatChat(newChatId, chatName, creator.Comp.UserId.Value, members, [], false);
        server.Value.Comp.Chats.Add(newChat);
        creator.Comp.SelectedChat = newChatId;

        _adminLogger.Add(LogType.Chat,
            LogImpact.Low,
            $"НаноЧат: {ToPrettyString(actor):user} создал чат \"{chatName}\" ({newChatId})");
        UpdateAllClients(server.Value);
    }

    /// <summary>
    /// Обрабатывает изменение названия существующего чата.
    /// </summary>
    private void HandleEditChat(Entity<NanoChatCartridgeComponent> sender, NanoChatEditChatEvent args)
    {
        if (sender.Comp.SelectedChat == null || sender.Comp.UserId == null)
            return;

        var server = GetServerForCartridge(sender);

        var chat = server?.Comp.Chats.FirstOrDefault(c => c.Id == args.ChatId
                                                          && c.Owner == sender.Comp.UserId.Value);
        if (server == null || chat == null)
            return;

        var oldName = chat.Name;
        chat.Name = args.NewName;

        _adminLogger.Add(LogType.Chat,
            LogImpact.Low,
            $"НаноЧат: {ToPrettyString(args.Actor):user} переименовал чат ({chat.Id}) \"{oldName}\" в \"{args.NewName}\"");

        Dirty(server.Value);
        UpdateAllClients(server.Value);
    }


    /// <summary>
    /// Добавляет новых участников в указанный чат.
    /// </summary>
    private void HandleAddMembers(Entity<NanoChatCartridgeComponent> ent, NanoChatAddMembersEvent args)
    {
        var server = GetServerForCartridge(ent);
        if (server == null || ent.Comp.UserId == null)
            return;

        var chat = server.Value.Comp.Chats.FirstOrDefault(c => c.Id == args.ChatId);
        if (chat == null || !chat.Members.Contains(ent.Comp.UserId.Value))
            return;

        if (chat.Automated)
        {
            var newMembers = chat.Members.ToList();
            var addedAnyNew = false;

            foreach (var newMember in args.AddedMembers.Where(newMember => !newMembers.Contains(newMember)))
            {
                newMembers.Add(newMember);
                addedAnyNew = true;
            }

            if (!addedAnyNew)
                return;

            var memberNames = newMembers
                .Select(id => server.Value.Comp.Users.FirstOrDefault(user => user.Id == id).Name ?? Loc.GetString("nano-chat-ui-chat-window-sender-unknown"))
                .ToList();

            var newChatName = string.Join(", ", memberNames);

            HandleCreateChat(ent, newChatName, args.Actor, newMembers);
            return;
        }

        var addedAny = false;
        var newGroupMembers = new List<NetEntity>();
        foreach (var newMember in args.AddedMembers.Where(newMember => !chat.Members.Contains(newMember)))
        {
            newGroupMembers.Add(newMember);
            addedAny = true;
        }

        if (!addedAny)
            return;

        chat.Members.AddRange(newGroupMembers);

        var groupAddedString = newGroupMembers.Aggregate("", (current, member) => current + $"{ToPrettyString(member)}, ");
        _adminLogger.Add(LogType.Chat, LogImpact.Low, $"НаноЧат: {ToPrettyString(args.Actor):user} добавил [ {groupAddedString} ] в чат \"{chat.Name}\"({chat.Id})");

        if (string.IsNullOrEmpty(chat.Name))
        {
            var memberNames = chat.Members
                .Select(id =>
                    server.Value.Comp.Users
                        .FirstOrDefault(user => user.Id == id)
                        .Name
                    ?? Loc.GetString("nano-chat-ui-chat-window-sender-unknown"))
                .ToList();

            chat.Name = string.Join(", ", memberNames);
        }

        Dirty(server.Value);
        UpdateAllClients(server.Value);
    }

    /// <summary>
    /// Удаляет участников из указанного чата.
    /// </summary>
    private void HandleRemoveMembers(Entity<NanoChatCartridgeComponent> ent, NanoChatRemoveMembersEvent args)
    {
        var server = GetServerForCartridge(ent);
        if (server == null || ent.Comp.UserId == null)
            return;

        var chat = server.Value.Comp.Chats.FirstOrDefault(c => c.Id == args.ChatId);
        if (chat == null)
            return;

        if (!chat.Members.Contains(ent.Comp.UserId.Value) || chat.Owner != ent.Comp.UserId.Value)
            return;

        var removed = args.RemovedMembers
            .Where(member => chat.Members.Contains(member))
            .ToList();

        foreach (var member in removed)
            chat.Members.Remove(member);

        if (removed.Count > 0)
        {
            var removedString = string.Join(", ",
                removed.Select(ToPrettyString));

            _adminLogger.Add(LogType.Chat,
                LogImpact.Low,
                $"НаноЧат: {ToPrettyString(args.Actor):user} удалил [ {removedString} ] из чата \"{chat.Name}\" ({chat.Id})");
        }

        Dirty(server.Value);
        UpdateAllClients(server.Value);
    }


    /// <summary>
    /// Отправляет текстовое сообщение в выбранный чат и рассылает уведомления участникам.
    /// </summary>
    private void HandleSendText(Entity<NanoChatCartridgeComponent> sender, string text, EntityUid actor)
    {
        if (string.IsNullOrWhiteSpace(text) || sender.Comp.SelectedChat == null || sender.Comp.UserId == null)
            return;

        var server = GetServerForCartridge(sender);
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
        Dirty(server.Value);

        var query = EntityQueryEnumerator<NanoChatCartridgeComponent>();

        _adminLogger.Add(LogType.Chat,
            LogImpact.Low,
            $"НаноЧат{(chat.Name is not "" ? $" {chat.Name} " : "")} ({chat.Id}) {ToPrettyString(actor):user}: {text}");

        while (query.MoveNext(out var clientUid, out var targetComp))
        {
            if (targetComp.ConnectedServer != server.Value.Owner)
                continue;

            if (targetComp.UserId == null)
                continue;

            if (!chat.Members.Contains(targetComp.UserId.Value) && targetComp.Visible)
                continue;

            if (targetComp.UserId == senderId)
                continue;

            if (targetComp.SelectedChat != chatId)
            {
                targetComp.UnreadMessages.TryAdd(chatId, 0);
                targetComp.UnreadMessages[chatId]++;
            }

            if (!targetComp.NotificationsOn)
                continue;

            var pda = Transform(clientUid).ParentUid;

            if (!TryComp<CartridgeLoaderComponent>(pda, out var loader) ||
                _userInterfaceSystem.IsUiOpen(pda, loader.UiKey))
            {
                _audio.PlayPvs(targetComp.NotificationSound, pda);
                continue;
            }

            var notifSender = chat.Automated
                ? (sender.Comp.PdaCardName ?? Loc.GetString("nano-chat-ui-chat-window-sender-unknown"))
                : chat.Name;
            var notifMessage = chat.Automated
                ? Loc.GetString("nano-chat-pda-notification-message",
                    ("sender", notifSender))
                : Loc.GetString("nano-chat-pda-notification-message-group",
                    ("sender", notifSender));

            _loaderSystem.SendNotification(
                pda,
                Loc.GetString("nano-chat-pda-notification-header"),
                notifMessage,
                loader);
        }

        UpdateAllClients(server.Value);
    }

    /// <summary>
    /// Регистрирует статус набора текста для текущего пользователя.
    /// </summary>
    private void HandleTyping(Entity<NanoChatCartridgeComponent> sender)
    {
        if (sender.Comp.SelectedChat == null || sender.Comp.UserId == null)
            return;

        var server = GetServerForCartridge(sender);
        if (server == null)
            return;

        var chatId = sender.Comp.SelectedChat.Value;
        var userId = sender.Comp.UserId.Value;

        if (!server.Value.Comp.TypingTimeouts.ContainsKey(chatId))
            server.Value.Comp.TypingTimeouts[chatId] = new Dictionary<NetEntity, TimeSpan>();

        server.Value.Comp.TypingTimeouts[chatId][userId] = _timing.CurTime + server.Value.Comp.TypingTimeout;

        Dirty(server.Value);
        UpdateAllClients(server.Value);
    }

    /// <summary>
    /// Распечатывает историю чата на бумаге с использованием штампа.
    /// </summary>
    private void HandlePrintChat(Entity<NanoChatCartridgeComponent> ent, NanoChatPrintEvent args)
    {
        if (ent.Comp.SelectedChat == null || ent.Comp.UserId == null)
            return;

        if (_timing.CurTime < ent.Comp.NextPrintAllowedAfter)
            return;

        var server = GetServerForCartridge(ent);

        var chat = server?.Comp.Chats.FirstOrDefault(c => c.Id == ent.Comp.SelectedChat.Value);
        if (chat == null)
            return;

        ent.Comp.NextPrintAllowedAfter = _timing.CurTime + ent.Comp.PrintDelay;

        var loaderUid = Transform(ent).ParentUid;
        var coordinates = Transform(loaderUid).Coordinates;

        var printed = Spawn(ent.Comp.PaperId, coordinates);
        _hands.PickupOrDrop(args.Actor, printed);

        _audio.PlayPvs(ent.Comp.PrintingSound, loaderUid);

        if (!TryComp<PaperComponent>(printed, out var paper))
            return;

        _metaDataSystem.SetEntityName(printed,
            Loc.GetString("nano-chat-print-name",
            ("name", Name(printed)),
            ("id", chat.Id)));


        var msg = new FormattedMessage();
        var chatName = string.IsNullOrEmpty(chat.Name)
            ? Loc.GetString("nano-chat-print-default-title")
            : chat.Name;

        var spacesCount = Math.Max(0, 19 - (chatName.Length / 2));
        var padding = new string(' ', spacesCount);

        if (!_prototype.TryIndex(ent.Comp.StampId, out var stampProto))
            return;

        if (!stampProto.TryGetComponent<StampComponent>(out var stamp, _componentFactory))
            return;

        var stampInfo = new StampDisplayInfo
        {
            StampedName = stamp.StampedName,
            StampedColor = stamp.StampedColor,
        };

        _paper.TryStamp((printed, paper), stampInfo, stamp.StampState);

        msg.AddMarkupOrThrow($"[head=3]{padding}{FormattedMessage.EscapeText(chatName)}");
        msg.PushNewline();

        foreach (var message in chat.Messages)
        {
            var timeString = message.SendTime.ToString(@"hh\:mm\:ss");

            msg.AddMarkupOrThrow($@"[bold]{FormattedMessage.EscapeText(message.SenderName)}[/bold] [color=#888888]\[{timeString}\][/color]:");
            msg.PushNewline();
            msg.AddMarkupOrThrow(FormattedMessage.EscapeText(message.Content));
            msg.PushNewline();
            msg.PushNewline();
        }

        _adminLogger.Add(LogType.Chat,
            LogImpact.Low,
            $"НаноЧат: {ToPrettyString(args.Actor):user} распечатал чат \"{chat.Name}\" ({chat.Id})");

        _paper.SetContent((printed, paper), msg.ToMarkup());
        UpdateUi(ent);
    }

    /// <summary>
    /// Синхронизирует активных пользователей на всех серверах, удаляя тех, кто отключился.
    /// </summary>
    private void SyncUsers()
    {
        var activeUsersByServer = new Dictionary<EntityUid, HashSet<NetEntity>>();

        var query = EntityQueryEnumerator<NanoChatCartridgeComponent>();

        while (query.MoveNext(out var uid, out var comp))
        {
            SyncUserToServer((uid, comp), out var server);

            if (server == null || comp.UserId == null)
                continue;

            activeUsersByServer.TryAdd(server.Value.Owner, new HashSet<NetEntity>());
            activeUsersByServer[server.Value.Owner].Add(comp.UserId.Value);
        }

        foreach (var (serverUid, activeUsers) in activeUsersByServer)
        {
            if (!TryComp<NanoChatServerComponent>(serverUid, out var serverComp))
                continue;

            var removedUsers = serverComp.Users
                .Where(id => !activeUsers.Contains(id.Id))
                .ToList();

            foreach (var user in removedUsers)
            {
                serverComp.Users.Remove(user);

                foreach (var chat in serverComp.Chats.Where(chat => chat.Members.Contains(user.Id)))
                    chat.Members.Remove(user.Id);
            }

            serverComp.Chats.RemoveAll(chat =>
                chat.Members.Count == 0);

            if (removedUsers.Count > 0)
                Dirty(serverUid, serverComp);
        }
    }



    /// <summary>
    /// Синхронизирует данные текущего картриджа с сервером.
    /// </summary>
    private void SyncUserToServer(Entity<NanoChatCartridgeComponent> ent, out Entity<NanoChatServerComponent>? server)
    {
        var loaderUid = Transform(ent).ParentUid;
        var pdaData = GetPdaInfo(loaderUid);

        if (pdaData.Id == null)
        {
            server = null;
            return;
        }

        ent.Comp.UserId = pdaData.Id.Value;
        ent.Comp.PdaCardName = pdaData.Name;

        server = GetServerForCartridge(ent);

        if (server == null)
            return;

        ent.Comp.ConnectedServer = server.Value.Owner;

        var contact = new NanoChatContact(
            pdaData.Id.Value,
            pdaData.Name ?? Loc.GetString("nano-chat-ui-chat-window-sender-unknown"),
            ent.Comp.Visible,
            pdaData.Job ?? Loc.GetString("nano-chat-ui-contact-job-unknown"),
            pdaData.Icon);

        var index = server.Value.Comp.Users.FindIndex(u => u.Id == pdaData.Id.Value);

        if (index >= 0)
            server.Value.Comp.Users[index] = contact;
        else
            server.Value.Comp.Users.Add(contact);

        if (!ent.Comp.Visible)
        {
            Dirty(server.Value);
            return;
        }
        foreach (var otherUser in server.Value.Comp.Users.Where(otherUser => otherUser.Id != pdaData.Id.Value && otherUser.Visible))
        {
            var exists = server.Value.Comp.Chats.Any(c =>
                c.Members.Count == 2 &&
                c.Members.Contains(pdaData.Id.Value) &&
                c.Members.Contains(otherUser.Id));

            if (exists)
                continue;

            server.Value.Comp.Chats.Add(
                new NanoChatChat(
                    server.Value.Comp.NextChatId++,
                    string.Empty,
                    null,
                    [
                        pdaData.Id.Value,
                        otherUser.Id,
                    ],
                    []));
        }

        Dirty(server.Value);
    }

    /// <summary>
    /// Обновляет UI всех клиентов.
    /// </summary>
    private void UpdateAllClients(Entity<NanoChatServerComponent> server)
    {
        SyncUsers();

        var query = EntityQueryEnumerator<NanoChatCartridgeComponent>();

        while (query.MoveNext(out var uid, out var comp))
        {
            SyncUserToServer((uid, comp), out var connectedServer);

            if (connectedServer?.Owner != server.Owner)
                continue;

            UpdateUi((uid, comp), connectedServer);
        }
    }

    /// <summary>
    /// Находит активный сервер НаноЧата на той же карте.
    /// </summary>
    private Entity<NanoChatServerComponent>? GetServerForCartridge(EntityUid cartridgeUid)
    {
        var cartTrans = Transform(cartridgeUid);

        var serverQuery = EntityQueryEnumerator<NanoChatServerComponent, TransformComponent, ApcPowerReceiverComponent>();
        while (serverQuery.MoveNext(out var serverUid, out var serverComponent, out var serverTransform, out var power))
        {
            if (serverTransform.MapID != cartTrans.MapID || !power.Powered)
                continue;

            if (serverComponent.CartridgeWhitelist != null && !_whitelist.IsValid(serverComponent.CartridgeWhitelist, cartridgeUid))
                continue;

            return (serverUid, serverComponent);
        }
        return null;
    }

    /// <summary>
    /// Извлекает данные пользователя из ID-карты, установленной в КПК.
    /// </summary>
    private (NetEntity? Id, string? Name, string? Job, ProtoId<JobIconPrototype>? Icon) GetPdaInfo(EntityUid pdaUid)
    {
        if (!TryComp<PdaComponent>(pdaUid, out var pda))
            return (null, null, null, null);

        var idCardUid = pda.ContainedId;
        if (idCardUid == null || !TryComp<IdCardComponent>(idCardUid, out var idCard))
            return (null, null, null, null);

        var fullName = string.IsNullOrEmpty(idCard.FullName) ? Loc.GetString("nano-chat-ui-chat-window-sender-unknown") : idCard.FullName;
        var jobTitle = string.IsNullOrEmpty(idCard.LocalizedJobTitle) ? Loc.GetString("nano-chat-ui-contact-job-unknown") : idCard.LocalizedJobTitle;
        var id = GetNetEntity(idCardUid.Value);
        return (id, fullName, jobTitle, idCard.JobIcon);
    }
}
