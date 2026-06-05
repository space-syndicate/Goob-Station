using Content.Server.CartridgeLoader;
using Content.Server.GameTicking;
using Content.Server.Station.Systems;
using Content.Server.Administration.Logs;
using Content.Shared._CorvaxGoob.FaxTracker;
using Content.Shared.CartridgeLoader;
using Content.Shared.Database;
using Content.Shared.DeviceNetwork.Components;
using Content.Shared.Fax.Components;
using Content.Shared.GameTicking;
using Content.Shared.Inventory;
using Content.Shared.Popups;
using System.Text.RegularExpressions;

namespace Content.Server._CorvaxGoob.FaxTracker;

public sealed class FaxTrackerCartridgeSystem : EntitySystem
{
    [Dependency] private readonly CartridgeLoaderSystem _cartridge = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly GameTicker _ticker = default!;
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FaxTrackerCartridgeComponent, CartridgeUiReadyEvent>(OnUiReady);
        SubscribeLocalEvent<FaxTrackerCartridgeComponent, CartridgeMessageEvent>(OnUiMessage);
        SubscribeLocalEvent<FaxTrackerCartridgeComponent, CartridgeAfterInteractEvent>(OnAfterInteract);

        SubscribeLocalEvent<FaxMessageReceivedEvent>(OnFaxReceived);
        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawn);
    }

    private void OnUiReady(EntityUid uid, FaxTrackerCartridgeComponent comp, CartridgeUiReadyEvent args)
    {
        UpdateUi((uid, comp), args.Loader);
    }

    private void OnUiMessage(EntityUid uid, FaxTrackerCartridgeComponent comp, CartridgeMessageEvent args)
    {
        if (args is not FaxTrackerUiMessageEvent message)
            return;

        switch (message.Action)
        {
            case FaxTrackerUiAction.ClearHistory:
                comp.History.RemoveAll(entry => !entry.IsFavorite);
                if (comp.History.Count == 0)
                    comp.ReceivedCount = 0;
                break;
            case FaxTrackerUiAction.ToggleNotifications:
                comp.NotificationsOn = !comp.NotificationsOn;
                break;
            case FaxTrackerUiAction.RemoveEntry:
                comp.History.RemoveAll(entry => entry.Number == message.Number);
                break;
            case FaxTrackerUiAction.ToggleFavorite:
                var favIndex = comp.History.FindIndex(entry => entry.Number == message.Number);
                if (favIndex >= 0)
                {
                    var entry = comp.History[favIndex];
                    entry.IsFavorite = !entry.IsFavorite;
                    comp.History[favIndex] = entry;
                }
                break;
            case FaxTrackerUiAction.BlacklistAdd:
                if (message.Address != null && !comp.BlacklistedSenders.ContainsKey(message.Address))
                    comp.BlacklistedSenders[message.Address] = ResolveFaxName(message.Address, comp);
                break;
            case FaxTrackerUiAction.BlacklistRemove:
                if (message.Address != null)
                    comp.BlacklistedSenders.Remove(message.Address);
                break;
        }

        UpdateUi((uid, comp), GetEntity(args.LoaderUid));
    }

    private void OnAfterInteract(EntityUid uid, FaxTrackerCartridgeComponent comp, CartridgeAfterInteractEvent args)
    {
        var interact = args.InteractEvent;
        if (interact.Handled || !interact.CanReach || interact.Target == null)
            return;

        var target = interact.Target.Value;
        if (!TryComp<FaxMachineComponent>(target, out var fax))
            return;

        comp.BoundFax = target;
        interact.Handled = true;

        _adminLogger.Add(LogType.PdaInteract, LogImpact.Low,
            $"{ToPrettyString(interact.User):user} bound fax tracker {ToPrettyString(args.Loader):tool} to \"{fax.FaxName}\" {ToPrettyString(target):subject}");

        _popup.PopupEntity(Loc.GetString("fax-tracker-bound", ("fax", fax.FaxName)), args.Loader, interact.User);
        UpdateUi((uid, comp), args.Loader);
    }

    private void OnFaxReceived(FaxMessageReceivedEvent args)
    {
        var query = EntityQueryEnumerator<FaxTrackerCartridgeComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.BoundFax != args.Fax)
                continue;

            if (args.SenderAddress != null && comp.BlacklistedSenders.ContainsKey(args.SenderAddress))
                continue;

            comp.ReceivedCount++;
            var title = ExtractTitle(args.Content);

            var senderName = args.SenderName;
            if (args.SenderAddress != null && TryGetFaxNameByAddress(args.SenderAddress, out var liveName))
                senderName = liveName;

            comp.History.Insert(0, new FaxTrackerEntry(comp.ReceivedCount, title, args.Content, args.Stamps, _ticker.RoundDuration(), senderName));

            while (comp.History.Count > comp.MaxHistory)
            {
                var removed = false;
                for (var i = comp.History.Count - 1; i >= 0; i--)
                {
                    if (comp.History[i].IsFavorite)
                        continue;

                    comp.History.RemoveAt(i);
                    removed = true;
                    break;
                }

                if (!removed)
                    break;
            }

            if (!TryComp<CartridgeComponent>(uid, out var cartridge) || cartridge.LoaderUid == null)
                continue;

            var loader = cartridge.LoaderUid.Value;
            if (comp.NotificationsOn)
            {
                _cartridge.SendNotification(loader,
                    Loc.GetString("fax-tracker-notification-header"),
                    title ?? args.PaperName);
            }

            UpdateUi((uid, comp), loader);
        }
    }

    private void OnPlayerSpawn(PlayerSpawnCompleteEvent ev)
    {
        if (ev.JobId == null)
            return;

        var jobId = ev.JobId;
        if (!_inventory.TryGetSlotEntity(ev.Mob, "id", out var pda))
            return;

        if (!_cartridge.TryGetProgram<FaxTrackerCartridgeComponent>(pda.Value, out var progUid, out var prog))
            return;

        if (!prog.JobFaxNames.TryGetValue(jobId, out var keywords))
            return;

        var stationFaxes = new List<Entity<FaxMachineComponent>>();
        var query = EntityQueryEnumerator<FaxMachineComponent>();
        while (query.MoveNext(out var faxUid, out var fax))
        {
            if (_station.GetOwningStation(faxUid) == ev.Station)
                stationFaxes.Add((faxUid, fax));
        }

        foreach (var keyword in keywords)
        {
            foreach (var fax in stationFaxes)
            {
                if (!fax.Comp.FaxName.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                    continue;

                prog.BoundFax = fax.Owner;

                if (TryComp<CartridgeComponent>(progUid.Value, out var cartridge) && cartridge.LoaderUid != null)
                    UpdateUi((progUid.Value, prog), cartridge.LoaderUid.Value);

                return;
            }
        }
    }

    private void UpdateUi(Entity<FaxTrackerCartridgeComponent> ent, EntityUid loaderUid)
    {
        string? faxName = null;
        var bound = false;
        FaxMachineComponent? faxComp = null;
        if (ent.Comp.BoundFax != null && TryComp(ent.Comp.BoundFax.Value, out faxComp))
        {
            bound = true;
            faxName = faxComp.FaxName;
        }

        var blacklist = new List<FaxTrackerSender>();
        foreach (var kv in ent.Comp.BlacklistedSenders)
            blacklist.Add(new FaxTrackerSender(kv.Key, kv.Value));

        var available = new List<FaxTrackerSender>();
        if (ent.Comp.BoundFax != null)
        {
            var boundFax = ent.Comp.BoundFax.Value;
            var station = _station.GetOwningStation(boundFax);
            var faxQuery = EntityQueryEnumerator<FaxMachineComponent, DeviceNetworkComponent>();
            while (faxQuery.MoveNext(out var otherUid, out var otherFax, out var net))
            {
                if (otherUid == boundFax || string.IsNullOrEmpty(net.Address))
                    continue;
                if (station != null && _station.GetOwningStation(otherUid) != station)
                    continue;
                if (ent.Comp.BlacklistedSenders.ContainsKey(net.Address))
                    continue;

                available.Add(new FaxTrackerSender(net.Address, otherFax.FaxName));
            }
        }

        var state = new FaxTrackerUiState(ent.Comp.History, ent.Comp.NotificationsOn, bound, faxName, blacklist, available);
        _cartridge.UpdateCartridgeUiState(loaderUid, state);
    }

    private string ResolveFaxName(string address, FaxTrackerCartridgeComponent comp)
    {
        if (TryGetFaxNameByAddress(address, out var liveName))
            return liveName;

        if (comp.BoundFax != null && TryComp<FaxMachineComponent>(comp.BoundFax.Value, out var bound)
            && bound.KnownFaxes.TryGetValue(address, out var known))
            return known;

        return address;
    }

    private bool TryGetFaxNameByAddress(string address, out string name)
    {
        var query = EntityQueryEnumerator<FaxMachineComponent, DeviceNetworkComponent>();
        while (query.MoveNext(out _, out var fax, out var net))
        {
            if (net.Address == address)
            {
                name = fax.FaxName;
                return true;
            }
        }

        name = string.Empty;
        return false;
    }

    private const int MaxTitleLength = 50;

    private static string? ExtractTitle(string content)
    {
        var text = Regex.Replace(content, @"\[.*?\]", "");
        var lines = text.Split('\n');

        for (var i = 0; i < lines.Length - 1; i++)
        {
            if (!lines[i].Contains("====="))
                continue;

            var title = lines[i + 1].Trim();
            if (title.Length == 0 || title.Contains("====="))
                continue;

            if (title.Length > MaxTitleLength)
                title = title.Substring(0, MaxTitleLength).TrimEnd() + "…";

            return title;
        }

        return null;
    }
}
