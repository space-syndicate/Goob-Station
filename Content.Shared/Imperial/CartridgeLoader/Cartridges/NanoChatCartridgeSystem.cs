using System.Linq;
using Content.Shared.Access.Components;
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
                ent.Comp.SelectedContact = select.Contact;
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

        if (string.IsNullOrWhiteSpace(text) || string.IsNullOrEmpty(comp.SelectedContact?.Name))
            return;

        EnsurePdaName(sender);

        var senderName = comp.PdaCardName ?? Loc.GetString("nano-chat-ui-unknown-sender");
        var targetName = comp.SelectedContact.Value.Name;

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

    private void AddMessageToHistory(
        Entity<NanoChatCartridgeComponent> ent,
        string contactName,
        string messageSender,
        string text)
    {
        if (!ent.Comp.ChatHistories.TryGetValue(contactName, out var history))
        {
            history = new List<NanoChatMessage>();
            ent.Comp.ChatHistories[contactName] = history;
        }

        history.Add(new NanoChatMessage(messageSender, text));
        Dirty(ent);
    }

    private void EnsurePdaName(Entity<NanoChatCartridgeComponent> ent)
    {
        if (TryComp<PdaComponent>(Transform(ent).ParentUid, out var pda) &&
            !string.IsNullOrEmpty(pda.OwnerName))
        {
            ent.Comp.PdaCardName = pda.OwnerName;
        }
    }

    private List<NanoChatContact> GetContacts(Entity<NanoChatCartridgeComponent> currentEnt)
    {
        var contacts = new Dictionary<string, NanoChatContact>();

        var query = EntityQueryEnumerator<NanoChatCartridgeComponent>();
        while (query.MoveNext(out var uid, out _))
        {
            if (uid == currentEnt.Owner)
                continue;

            var loaderUid = Transform(uid).ParentUid;
            var (name, job) = GetPdaData(loaderUid);

            if (!string.IsNullOrEmpty(name))
                contacts[name] = new NanoChatContact(name, job);
        }

        foreach (var historicContact in currentEnt.Comp.ChatHistories.Keys.Where(historicContact => !contacts.ContainsKey(historicContact)))
            contacts[historicContact] = new NanoChatContact(historicContact, Loc.GetString("nano-chat-ui-contact-job-unknown"));

        var list = contacts.Values.ToList();
        list.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.Ordinal));
        return list;
    }

    private void UpdateUi(Entity<NanoChatCartridgeComponent> ent)
    {
        var comp = ent.Comp;
        var loaderUid = Transform(ent).ParentUid;

        if (!loaderUid.IsValid())
            return;

        EnsurePdaName(ent);

        var history = comp.SelectedContact is { } contact &&
                      comp.ChatHistories.TryGetValue(contact.Name, out var chatHistory)
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

    private (string? Name, string? Job) GetPdaData(EntityUid pdaUid, PdaComponent? pda = null)
    {
        if (!Resolve(pdaUid, ref pda, false))
            return (null, null);

        var name = pda.OwnerName;
        string? job = null;

        if (pda.ContainedId.HasValue && TryComp<IdCardComponent>(pda.ContainedId.Value, out var idCard))
        {
            Log.Debug($"idcard found: {pda.ContainedId.Value}. Title: {idCard.LocalizedJobTitle}");
            job = idCard.LocalizedJobTitle;
        }

        return (name, job);
    }
}
