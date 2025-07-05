using Content.Server.Administration.Logs;
using Content.Server.Chat.Systems;
using Content.Server.Chat.Managers;
using Content.Server.Fax;
using Content.Server.Station.Systems;
using Content.Shared.Imperial.JanitorsResponseTeam.Components;
using Content.Shared.DeviceNetwork.Events;
using Content.Shared.DeviceNetwork.Components;
using Content.Shared.Database;
using Content.Shared.Fax.Components;
using Content.Shared.Paper;
using Content.Server.StationRecords.Systems;
using Content.Shared.StationRecords;
using Robust.Shared.Console;
using System.Text.RegularExpressions;
using System.Linq;

namespace Content.Server.Imperial.JanitorsResponseTeam;

public sealed class CallJRTViaCentcomFaxSystem : EntitySystem
{
    [Dependency] private readonly IConsoleHost _consoleHost = default!;
    [Dependency] private readonly IAdminLogManager _adminLog = default!;
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly ChatSystem _chatSystem = default!;
    [Dependency] private readonly StationSystem _stationSystem = default!;
    [Dependency] private readonly StationRecordsSystem _stationRecordsSystem = default!;

    private readonly Regex _patternJRTRequest1 = new(
        @"ЗАПРОСНАВЫЗОВУБОРЩИКОВ",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private readonly Regex _patternJRTRequest2 = new(
        @"БЫСТРОГОРЕАГИРОВАНИЯ",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private readonly Regex _patternShape = new(
        @"ФОРМА:NT\-([A-Z]{3})\-SOD-REQ",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private readonly Regex _patternStationName = new(
        @"СТАНЦИЯ:NT14\-([A-Z]{2})\-(\d{3})",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private readonly Regex _patternDate = new(
        @"ДАТА\:(0[1-9]|[12][0-9]|3[01])\/(0[1-9]|1[0-2])\/([0-9]{4})",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private readonly Regex _patternNamePlayer = new(
        @"ПОДОТЧЁТНОЕ\s*ЛИЦО\s*:\s*([а-яА-ЯёЁ\s-]+)\s*ДОЛЖНОСТЬ",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private readonly Regex _patternProfession = new(
        @"ДОЛЖНОСТЬ\s*:\s*([а-яА-ЯёЁ\s-]+)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private bool _isThereIsJRT = false; // So that space assholes don't spam with the valid document

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CallJRTViaCentcomFaxComponent, DeviceNetworkPacketEvent>(OnPacketReceived);
    }

    private void OnPacketReceived(EntityUid uid, CallJRTViaCentcomFaxComponent component, DeviceNetworkPacketEvent args)
    {
        if (!HasComp<DeviceNetworkComponent>(uid))
            return;

        if (!args.Data.TryGetValue(FaxConstants.FaxPaperNameData, out string? name) ||
            !args.Data.TryGetValue(FaxConstants.FaxPaperContentData, out string? content))
            return;

        args.Data.TryGetValue(FaxConstants.FaxPaperLabelData, out string? label);
        args.Data.TryGetValue(FaxConstants.FaxPaperStampStateData, out string? stampState);
        args.Data.TryGetValue(FaxConstants.FaxPaperStampedByData, out List<StampDisplayInfo>? stampedBy);
        args.Data.TryGetValue(FaxConstants.FaxPaperPrototypeData, out string? prototypeId);
        args.Data.TryGetValue(FaxConstants.FaxPaperLockedData, out bool? locked);

        var printout = new FaxPrintout(content, name, label, prototypeId, stampState, stampedBy, locked ?? false);

        Receive(uid, printout);
    }

    public void Receive(EntityUid uid, FaxPrintout printout, CallJRTViaCentcomFaxComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        /// If this squad has been called more than 1 time, then this is spam
        if (_isThereIsJRT)
        {
            _adminLog.Add(LogType.Action, LogImpact.High, $"[CentcomFax] Attempt to spam papers about the call JRT.");
            _chatManager.SendAdminAnnouncement(Loc.GetString("admin-manager-fax-jrt-spam"));
            SendCentcomAnnouncement(Loc.GetString("centcom-announcement-jrt-spam"), component);
            return;
        }

        var text = printout.Content
            .Replace(" ", "")
            .Replace("[bold]", "")
            .Replace("[/bold]", "")
            .ToUpperInvariant();

        if (!IsValidDocument(uid, text, printout, component))
            return;
        if (!IsPollutedStation(component))
            return;

        _consoleHost.ExecuteCommand("callert JRT");
        _chatManager.SendAdminAnnouncement(Loc.GetString("admin-manager-fax-jrt-accepted"));
        _adminLog.Add(LogType.Action, LogImpact.Medium, $"[CentcomFax] JRT has been called.");

        _isThereIsJRT = true;
    }


    private bool IsValidDocument(EntityUid uid, string text, FaxPrintout printout, CallJRTViaCentcomFaxComponent component)
    {
        if (!IsCallingJRT(text))
            return false;
        if (!IsDocumentShape(text, component))
            return false;
        if (!IsStationNumber(text, component))
            return false;
        if (!IsDate(text, component))
            return false;
        if (!IsNamePlayer(uid, text, component))
            return false;
        if (!IsProfession(uid, text, component))
            return false;
        if (!IsStamp(printout, component))
            return false;

        return true;
    }



    public bool IsPollutedStation(CallJRTViaCentcomFaxComponent component)
    {
        int counter = 0;

        counter += ProcessTrashType(TrashComponent.TrashSize.Small, component.AmountTrashSmall);
        counter += ProcessTrashType(TrashComponent.TrashSize.Medium, component.AmountTrashMedium);
        counter += ProcessTrashType(TrashComponent.TrashSize.Large, component.AmountTrashLarge);

        _adminLog.Add(LogType.Action, LogImpact.High, $"[CentcomFax] Total trash score: {counter} (Threshold: {component.MinAmountTrash})");
        if (counter >= component.MinAmountTrash)
        {
            return true;
        }
        else
        {
            _adminLog.Add(LogType.Action, LogImpact.Medium, $"[CentcomFax] The amount of garbage at the station does not exceed the minimum limit for sending JRT.");
            return false;
        }
    }

    private int ProcessTrashType(TrashComponent.TrashSize size, int trashValue)
    {
        int count = 0;

        var query = EntityQueryEnumerator<TrashComponent, TransformComponent>();
        while (query.MoveNext(out var _, out var trash, out var xform))
        {
            if (trash.Size != size) continue;
            if (xform.GridUid == EntityUid.Invalid) continue;

            var station = _stationSystem.GetOwningStation(xform.GridUid);
            if (station == null) continue;

            count += trashValue;
        }

        return count;
    }


    private bool IsCallingJRT(string text)
    {
        var ertRequest1 = _patternJRTRequest1.Match(text);
        if (!ertRequest1.Success)
            return false;

        var ertRequest2 = _patternJRTRequest2.Match(text);
        if (!ertRequest2.Success)
            return false;

        _chatManager.SendAdminAnnouncement(Loc.GetString("admin-manager-fax-jrt-sent"));
        _adminLog.Add(LogType.Action, LogImpact.Low, $"[CentcomFax] An attempt to send a document for a call JRT.");
        return true;
    }

    private bool IsDocumentShape(string text, CallJRTViaCentcomFaxComponent component)
    {
        var shape = _patternShape.Match(text);
        if (!shape.Success)
        {
            _adminLog.Add(LogType.Action, LogImpact.Low, $"[CentcomFax] Incorrect document FORM. The correct format: NT-XXX-SOD-REQ");
            SendCentcomAnnouncement(Loc.GetString("centcom-announcement-jrt-invalid-document"), component);
            return false;
        }
        return true;
    }

    private bool IsStationNumber(string text, CallJRTViaCentcomFaxComponent component)
    {
        var stationName = _patternStationName.Match(text);
        if (!stationName.Success)
        {
            _adminLog.Add(LogType.Action, LogImpact.Low, $"[CentcomFax] The station format is incorrect. The correct format: NT14-XX-###");
            SendCentcomAnnouncement(Loc.GetString("centcom-announcement-jrt-invalid-document"), component);
            return false;
        }
        return true;
    }

    private bool IsDate(string text, CallJRTViaCentcomFaxComponent component)
    {
        var date = _patternDate.Match(text);
        if (!date.Success)
        {
            _adminLog.Add(LogType.Action, LogImpact.Low, $"[CentcomFax] Incorrect date format. The correct format: dd/mm/yyyy");
            SendCentcomAnnouncement(Loc.GetString("centcom-announcement-jrt-invalid-document"), component);
            return false;
        }
        return true;
    }

    private bool IsNamePlayer(EntityUid uid, string text, CallJRTViaCentcomFaxComponent component)
    {
        var nameMatch = _patternNamePlayer.Match(text);
        if (!nameMatch.Success)
        {
            _adminLog.Add(LogType.Action, LogImpact.Low, $"[CentcomFax] The 'Подотчётное лицо' field was not found in the document.");
            SendCentcomAnnouncement(Loc.GetString("centcom-announcement-jrt-invalid-document"), component);
            return false;
        }


        string rawName = nameMatch.Groups[1].Value.Trim();
        if (string.IsNullOrWhiteSpace(rawName))
            return false;


        var station = _stationSystem.GetOwningStation(uid);
        if (station == null)
            return false;

        string normalizedInputName = rawName
            .Replace(" ", "")
            .ToUpperInvariant();

        var allRecords = _stationRecordsSystem.GetRecordsOfType<GeneralStationRecord>(station.Value);
        foreach (var (_, record) in allRecords)
        {
            string normalizedRecordName = record.Name.Replace(" ", "").ToUpperInvariant();

            if (normalizedRecordName == normalizedInputName)
            {
                return true;
            }
        }

        _adminLog.Add(LogType.Action, LogImpact.Low, $"[CentcomFax] The name '{rawName}' was not found in the station records. Total records checked: {allRecords.Count()}.");
        SendCentcomAnnouncement(Loc.GetString("centcom-announcement-jrt-invalid-document"), component);
        return false;
    }

    private bool IsProfession(EntityUid uid, string text, CallJRTViaCentcomFaxComponent component)
    {
        var profession = _patternProfession.Match(text);
        if (!profession.Success)
        {
            _adminLog.Add(LogType.Action, LogImpact.Low, $"[CentcomFax] Incorrect profession.");
            SendCentcomAnnouncement(Loc.GetString("centcom-announcement-jrt-invalid-document"), component);
            return false;
        }

        string rawProfession = profession.Groups[1].Value.Trim();
        if (string.IsNullOrWhiteSpace(rawProfession))
            return false;


        var station = _stationSystem.GetOwningStation(uid);
        if (station == null)
            return false;

        string normalizedInputProfession = rawProfession
            .Replace(" ", "")
            .ToUpperInvariant();

        var allRecords = _stationRecordsSystem.GetRecordsOfType<GeneralStationRecord>(station.Value);
        foreach (var (_, record) in allRecords)
        {
            string normalizedRecordProfession = record.JobTitle.Replace(" ", "").ToUpperInvariant();
            if (normalizedRecordProfession == normalizedInputProfession)
            {
                return true;
            }
        }

        _adminLog.Add(LogType.Action, LogImpact.Low, $"[CentcomFax] Profession '{rawProfession}' was not found in the station's records. Total records checked: {allRecords.Count()}.");
        SendCentcomAnnouncement(Loc.GetString("centcom-announcement-jrt-invalid-document"), component);
        return false;
    }

    private bool IsStamp(FaxPrintout printout, CallJRTViaCentcomFaxComponent component)
    {
        if (printout.StampedBy.Count == 0)
        {
            _adminLog.Add(LogType.Action, LogImpact.Low, $"[CentcomFax] There are no seals. The correct format is the seal of the captain or the cleaner (then ONLY the cleaner puts the seal).");
            SendCentcomAnnouncement(Loc.GetString("centcom-announcement-jrt-invalid-document"), component);
            return false;
        }

        bool hasCaptainStamp = printout.StampedBy.Any(s => s.StampedName == "Капитан");
        bool hasMopStamp = printout.StampedBy.Any(s => s.StampedName == "Уборщик");
        if (hasCaptainStamp || hasMopStamp)
        {
            _adminLog.Add(LogType.Action, LogImpact.Low, $"[CentcomFax] Incorrect printing. The correct format is the seal of the captain or the cleaner (then ONLY the cleaner puts the seal).");
            SendCentcomAnnouncement(Loc.GetString("centcom-announcement-jrt-invalid-document"), component);
            return false;
        }
        return true;
    }

    private void SendCentcomAnnouncement(string message, CallJRTViaCentcomFaxComponent comp)
    {
        _chatSystem.DispatchGlobalAnnouncement(
            message,
            sender: Loc.GetString("centcom-announcement-jrt-sender-name"),
            true,
            comp.Sound,
            colorOverride: Color.Gold
        );
    }
}
