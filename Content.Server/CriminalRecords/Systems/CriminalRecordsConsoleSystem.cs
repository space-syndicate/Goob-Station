// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Access.Systems;
using Content.Server.GameTicking;
using Content.Server.Popups;
using Content.Server.Radio.EntitySystems;
using Content.Server.Station.Systems;
using Content.Server.StationRecords;
using Content.Server.StationRecords.Systems;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.CriminalRecords;
using Content.Shared.CriminalRecords.Components;
using Content.Shared.CriminalRecords.Systems;
using Content.Shared.GameTicking;
using Content.Shared.IdentityManagement;
using Content.Shared.Paper;
using Content.Shared.Security;
using Content.Shared.StationRecords;
using Robust.Server.Audio;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Timing;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Content.Server.CriminalRecords.Systems;

/// <summary>
/// Handles all UI for criminal records console
/// </summary>
public sealed partial class CriminalRecordsConsoleSystem : SharedCriminalRecordsConsoleSystem
{
    [Dependency] private readonly AccessReaderSystem _access = default!;
    [Dependency] private readonly CriminalRecordsSystem _criminalRecords = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly RadioSystem _radio = default!;
    [Dependency] private readonly StationRecordsSystem _records = default!;
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;

    // CorvaxGoob-SecurityFeatures-Start
    [Dependency] private readonly SharedGameTicker _gameTicker = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IdCardSystem _idCard = default!;
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly PaperSystem _paperSystem = default!;
    // CorvaxGoob-SecurityFeatures-End

    public override void Initialize()
    {
        SubscribeLocalEvent<CriminalRecordsConsoleComponent, RecordModifiedEvent>(UpdateUserInterface);
        SubscribeLocalEvent<CriminalRecordsConsoleComponent, AfterGeneralRecordCreatedEvent>(UpdateUserInterface);

        Subs.BuiEvents<CriminalRecordsConsoleComponent>(CriminalRecordsConsoleKey.Key, subs =>
        {
            subs.Event<BoundUIOpenedEvent>(UpdateUserInterface);
            subs.Event<SelectStationRecord>(OnKeySelected);
            subs.Event<SetStationRecordFilter>(OnFiltersChanged);
            subs.Event<CriminalRecordChangeStatus>(OnChangeStatus);
            subs.Event<CriminalRecordChangeDetainedStatus>(OnChangeDetainedStatus); // CorvaxGoob-SecurityFeatures
            subs.Event<CriminalRecordChangeWantedStatus>(OnChangeWantedStatus); // CorvaxGoob-SecurityFeatures
            subs.Event<CriminalRecordAddHistory>(OnAddHistory);
            subs.Event<CriminalRecordDeleteHistory>(OnDeleteHistory);
            subs.Event<CriminalRecordPrint>(OnPrint); // CorvaxGoob-SecurityFeatures
            subs.Event<CriminalRecordSetStatusFilter>(OnStatusFilterPressed);
        });

        Subs.BuiEvents<IdExaminableComponent>(SetWantedVerbMenu.Key, subs => // Goobstation-WantedMenu
        {
            subs.Event<BoundUIOpenedEvent>(UpdateUserInterface);
            subs.Event<CriminalRecordChangeStatus>(OnChangeStatus);
            subs.Event<CriminalRecordChangeDetainedStatus>(OnChangeDetainedStatus); // CorvaxGoob-SecurityFeatures
        });
    }

    private void UpdateUserInterface<T>(Entity<CriminalRecordsConsoleComponent> ent, ref T args)
    {
        // TODO: this is probably wasteful, maybe better to send a message to modify the exact state?
        UpdateUserInterface(ent);
    }

    private void OnKeySelected(Entity<CriminalRecordsConsoleComponent> ent, ref SelectStationRecord msg)
    {
        // no concern of sus client since record retrieval will fail if invalid id is given
        ent.Comp.ActiveKey = msg.SelectedKey;
        UpdateUserInterface(ent);
    }
    private void OnStatusFilterPressed(Entity<CriminalRecordsConsoleComponent> ent, ref CriminalRecordSetStatusFilter msg)
    {
        ent.Comp.FilterStatus = msg.FilterStatus;
        UpdateUserInterface(ent);
    }

    private void OnFiltersChanged(Entity<CriminalRecordsConsoleComponent> ent, ref SetStationRecordFilter msg)
    {
        if (ent.Comp.Filter == null ||
            ent.Comp.Filter.Type != msg.Type || ent.Comp.Filter.Value != msg.Value)
        {
            ent.Comp.Filter = new StationRecordsFilter(msg.Type, msg.Value);
            UpdateUserInterface(ent);
        }
    }

    private void GetOfficer(EntityUid uid, out string officer)
    {
        var tryGetIdentityShortInfoEvent = new TryGetIdentityShortInfoEvent(null, uid);
        RaiseLocalEvent(tryGetIdentityShortInfoEvent);
        officer = tryGetIdentityShortInfoEvent.Title ?? Loc.GetString("criminal-records-console-unknown-officer");
    }

    private void OnChangeStatus(Entity<CriminalRecordsConsoleComponent> ent, ref CriminalRecordChangeStatus msg)
    {
        // prevent malf client violating wanted/reason nullability
        if (msg.Status == SecurityStatus.Wanted != (msg.Reason != null) &&
            msg.Status == SecurityStatus.Suspected != (msg.Reason != null) &&
            msg.Status == SecurityStatus.Hostile != (msg.Reason != null) &&
            msg.Status == SecurityStatus.Search != (msg.Reason != null) && // Goobstation
            msg.Status == SecurityStatus.Dangerous != (msg.Reason != null) &&  // Goobstation
            msg.Status == SecurityStatus.Demote != (msg.Reason != null)) // Goobstation
            return;

        if (!CheckSelected(ent, msg.Actor, out var mob, out var key))
            return;

        if (!_records.TryGetRecord<CriminalRecord>(key.Value, out var record) || record.Status == msg.Status)
            return;

        // validate the reason
        string? reason = null;
        if (msg.Reason != null)
        {
            reason = msg.Reason.Trim();
            if (reason.Length < 1 || reason.Length > ent.Comp.MaxStringLength)
                return;
        }

        var oldStatus = record.Status;

        var name = _records.RecordName(key.Value);
        GetOfficer(mob.Value, out var officer);

        // when arresting someone add it to history automatically
        // fallback exists if the player was not set to wanted beforehand

        // CorvaxGoob-SecurityFeatures
        //if (msg.Status == SecurityStatus.Detained)
        //{
        //    var oldReason = record.Reason ?? Loc.GetString("criminal-records-console-unspecified-reason");
        //    var history = Loc.GetString("criminal-records-console-auto-history", ("reason", oldReason));
        //    _criminalRecords.TryAddHistory(key.Value, history, officer);
        //}

        // will probably never fail given the checks above
        name = _records.RecordName(key.Value);
        officer = Loc.GetString("criminal-records-console-unknown-officer");
        var jobName = "Unknown";

        _records.TryGetRecord<GeneralStationRecord>(key.Value, out var entry);
        if (entry != null)
            jobName = entry.JobTitle;

        var tryGetIdentityShortInfoEvent = new TryGetIdentityShortInfoEvent(null, mob.Value);
        RaiseLocalEvent(tryGetIdentityShortInfoEvent);
        if (tryGetIdentityShortInfoEvent.Title != null)
            officer = tryGetIdentityShortInfoEvent.Title;

        _criminalRecords.TryChangeStatus(key.Value, msg.Status, msg.Reason, officer);

        (string, object)[] args;
        if (reason != null)
            args = new (string, object)[] { ("name", name), ("officer", officer), ("reason", reason), ("job", jobName) };
        else
            args = new (string, object)[] { ("name", name), ("officer", officer), ("job", jobName) };

        // figure out which radio message to send depending on transition
        var statusString = (oldStatus, msg.Status) switch
        {
            (_, SecurityStatus.Hostile) => "hostile",
            (_, SecurityStatus.Eliminated) => "eliminated",
            // person has been detained
            (_, SecurityStatus.Detained) => "detained",
            // person did something sus
            (_, SecurityStatus.Suspected) => "suspected",
            // released on parole
            (_, SecurityStatus.Paroled) => "paroled",
            // prisoner did their time
            (_, SecurityStatus.Discharged) => "released",
            // going from any other state to wanted, AOS or prisonbreak / lazy secoff never set them to released and they reoffended
            (_, SecurityStatus.Wanted) => "wanted",
            (SecurityStatus.Hostile, SecurityStatus.None) => "not-hostile",
            (SecurityStatus.Eliminated, SecurityStatus.None) => "not-eliminated",
            // person has been sentenced to perma
            (_, SecurityStatus.Perma) => "perma", // Goobstation
            // person needs to be searched
            (_, SecurityStatus.Search) => "search", // Goobstation
            // person is very dangerous
            (_, SecurityStatus.Dangerous) => "dangerous", // Goobstation
            // person is demoted from their job
            (_, SecurityStatus.Demote) => "demote", // Goobstation
            // person is no longer sus
            (SecurityStatus.Suspected, SecurityStatus.None) => "not-suspected",
            // going from wanted to none, must have been a mistake
            (SecurityStatus.Wanted, SecurityStatus.None) => "not-wanted",
            // criminal status removed
            (SecurityStatus.Detained, SecurityStatus.None) => "released",
            // criminal is no longer on parole
            (SecurityStatus.Paroled, SecurityStatus.None) => "not-parole",
            // criminal is no longer in perma
            (SecurityStatus.Perma, SecurityStatus.None) => "not-perma", // Goobstation
            // person no longer needs to be searched
            (SecurityStatus.Search, SecurityStatus.None) => "not-search", // Goobstation
            // person is no longer dangerous
            (SecurityStatus.Dangerous, SecurityStatus.None) => "not-dangerous", // Goobstation
            // person no longer demoted
            (SecurityStatus.Demote, SecurityStatus.None) => "not-demoted", // Goobstation
            // this is impossible
            _ => "not-wanted"
        };

        // CorvaxGoob-SecurityFeatures : Записывается любое изменение статуса
        _criminalRecords.TryAddHistory(key.Value, Loc.GetString($"criminal-records-console-history",
            ("status", Loc.GetString($"criminal-records-status-{statusString}")),
            ("reason", reason ?? Loc.GetString($"criminal-records-console-unspecified"))), officer, status: msg.Status);

        _radio.SendRadioMessage(ent, Loc.GetString($"criminal-records-console-{statusString}", args),
            ent.Comp.SecurityChannel, ent);

        UpdateUserInterface(ent);
    }

    // CorvaxGoob-SecurityFeatures
    private void OnChangeDetainedStatus(Entity<CriminalRecordsConsoleComponent> ent, ref CriminalRecordChangeDetainedStatus msg)
    {
        if (!CheckSelected(ent, msg.Actor, out var mob, out var key))
            return;

        if (!_records.TryGetRecord<CriminalRecord>(key.Value, out var record))
            return;

        string? articles = null;

        if (msg.Articles != null)
        {
            articles = msg.Articles.Trim();
            if (articles.Length < 1 || articles.Length > ent.Comp.MaxStringLength)
                return;
        }

        if (msg.Duration is not null)
        {
            if (msg.Duration <= 0)
                return;
        }

        var name = _records.RecordName(key.Value);
        GetOfficer(mob.Value, out var officer);

        var history = Loc.GetString("criminal-records-console-detained-record", ("articles", articles ?? Loc.GetString("criminal-records-console-unspecified")), ("duration", msg.Duration?.ToString() ?? Loc.GetString("criminal-records-console-unspecified")));
        _criminalRecords.TryAddHistory(key.Value, history, officer, articles, msg.Duration, status: SecurityStatus.Detained);

        // will probably never fail given the checks above
        name = _records.RecordName(key.Value);

        officer = Loc.GetString("criminal-records-console-unknown-officer");
        var jobName = "Unknown";

        _records.TryGetRecord<GeneralStationRecord>(key.Value, out var entry);
        if (entry != null)
            jobName = entry.JobTitle;

        var tryGetIdentityShortInfoEvent = new TryGetIdentityShortInfoEvent(null, mob.Value);
        RaiseLocalEvent(tryGetIdentityShortInfoEvent);
        if (tryGetIdentityShortInfoEvent.Title != null)
            officer = tryGetIdentityShortInfoEvent.Title;

        _criminalRecords.TryChangeStatus(key.Value, SecurityStatus.Detained, articles, officer);

        (string, object)[] args;
        if (articles != null)
            args = new (string, object)[] { ("name", name), ("officer", officer), ("reason", articles), ("job", jobName) };
        else
            args = new (string, object)[] { ("name", name), ("officer", officer), ("job", jobName) };

        _radio.SendRadioMessage(ent, Loc.GetString($"criminal-records-console-detained", args),
            ent.Comp.SecurityChannel, ent);

        if (msg.Print && entry is not null)
        {
            ent.Comp.NextPrintTime = _timing.CurTime + ent.Comp.PrintCooldown;

            PrintDetainedDocument(ent, msg.Actor, entry, articles, msg.Duration);
            Dirty(ent);
        }

        UpdateUserInterface(ent);
    }

    // CorvaxGoob-SecurityFeatures
    private void OnChangeWantedStatus(Entity<CriminalRecordsConsoleComponent> ent, ref CriminalRecordChangeWantedStatus msg)
    {
        if (!CheckSelected(ent, msg.Actor, out var mob, out var key))
            return;

        if (!_records.TryGetRecord<CriminalRecord>(key.Value, out var record))
            return;

        var name = _records.RecordName(key.Value);
        GetOfficer(mob.Value, out var officer);

        name = _records.RecordName(key.Value);

        officer = Loc.GetString("criminal-records-console-unknown-officer");
        var jobName = "Unknown";

        _records.TryGetRecord<GeneralStationRecord>(key.Value, out var entry);
        if (entry != null)
            jobName = entry.JobTitle;

        var tryGetIdentityShortInfoEvent = new TryGetIdentityShortInfoEvent(null, mob.Value);
        RaiseLocalEvent(tryGetIdentityShortInfoEvent);
        if (tryGetIdentityShortInfoEvent.Title != null)
            officer = tryGetIdentityShortInfoEvent.Title;

        _criminalRecords.TryChangeStatus(key.Value, SecurityStatus.Wanted, msg.Reason, officer);

        string? reason = null;
        if (msg.Reason != null)
        {
            reason = msg.Reason.Trim();
            if (reason.Length < 1 || reason.Length > ent.Comp.MaxStringLength)
                return;
        }

        (string, object)[] args;
        if (reason != null)
            args = new (string, object)[] { ("name", name), ("officer", officer), ("reason", reason), ("job", jobName) };
        else
            args = new (string, object)[] { ("name", name), ("officer", officer), ("job", jobName) };


        _criminalRecords.TryAddHistory(key.Value, Loc.GetString($"criminal-records-console-history",
            ("status", Loc.GetString($"criminal-records-status-wanted")),
            ("reason", reason ?? Loc.GetString($"criminal-records-console-unspecified"))), officer, status: SecurityStatus.Wanted);

        _radio.SendRadioMessage(ent, Loc.GetString($"criminal-records-console-wanted", args),
            ent.Comp.SecurityChannel, ent);

        if (msg.Print && entry is not null)
        {
            ent.Comp.NextPrintTime = _timing.CurTime + ent.Comp.PrintCooldown;

            PrintWantedDocument(ent, msg.Actor, entry);
            Dirty(ent);
        }

        UpdateUserInterface(ent);
    }

    private void PrintDetainedDocument(Entity<CriminalRecordsConsoleComponent> ent, EntityUid officer, GeneralStationRecord record, string? articles, int? duration)
    {
        var content = Loc.GetString("doc-text-printer-sentence");

        var station = _station.GetOwningStation(ent);
        var stationName = station != null ? Name(station.Value) : null;

        var time = _gameTicker.RoundDuration().ToString("hh\\:mm\\:ss") + " " + DateTime.Now.AddYears(1000).ToShortDateString();

        content = content
            .Replace(Loc.GetString("doc-var-station"), stationName ?? Loc.GetString("doc-text-printer-default-station"))
            .Replace(Loc.GetString("doc-var-date"), time);

        if (_idCard.TryFindIdCard(officer, out var idCard))
        {
            content = content
            .Replace(Loc.GetString("doc-var-name"), idCard.Comp.FullName ?? Loc.GetString("doc-text-printer-default-name"))
            .Replace(Loc.GetString("doc-var-job"), idCard.Comp.LocalizedJobTitle ?? Loc.GetString("doc-text-printer-default-job"));
        }

        content = content
            .Replace(Loc.GetString("doc-var-violator"), record.Name)
            .Replace(Loc.GetString("doc-var-violator-job"), record.JobTitle)
            .Replace(Loc.GetString("doc-var-articles"), articles ?? Loc.GetString("doc-text-printer-default-articles"))
            .Replace(Loc.GetString("doc-var-duration"), duration.ToString())
            .Replace(Loc.GetString("doc-var-duration-start"), time);

        var printed = Spawn("Paper", Transform(ent).Coordinates);

        if (HasComp<PaperComponent>(printed))
        {
            _paperSystem.SetContent(printed, content);
            _audio.PlayPvs(new SoundPathSpecifier("/Audio/Machines/printer.ogg"), ent);
        }
    }

    // CorvaxGoob-SecurityFeatures
    private void PrintWantedDocument(Entity<CriminalRecordsConsoleComponent> ent, EntityUid officer, GeneralStationRecord record)
    {
        var content = Loc.GetString("doc-text-printer-closing-indictment");

        var station = _station.GetOwningStation(ent);
        var stationName = station != null ? Name(station.Value) : null;

        var time = _gameTicker.RoundDuration().ToString("hh\\:mm\\:ss") + " " + DateTime.Now.AddYears(1000).ToShortDateString();

        content = content
            .Replace(Loc.GetString("doc-var-station"), stationName ?? Loc.GetString("doc-text-printer-default-station"))
            .Replace(Loc.GetString("doc-var-date"), time);

        if (_idCard.TryFindIdCard(officer, out var idCard))
        {
            content = content
            .Replace(Loc.GetString("doc-var-name"), idCard.Comp.FullName ?? Loc.GetString("doc-text-printer-default-name"))
            .Replace(Loc.GetString("doc-var-job"), idCard.Comp.LocalizedJobTitle ?? Loc.GetString("doc-text-printer-default-job"));
        }

        var printed = Spawn("Paper", Transform(ent).Coordinates);

        if (HasComp<PaperComponent>(printed))
        {
            _paperSystem.SetContent(printed, content);
            _audio.PlayPvs(new SoundPathSpecifier("/Audio/Machines/printer.ogg"), ent);
        }
    }

    private void OnAddHistory(Entity<CriminalRecordsConsoleComponent> ent, ref CriminalRecordAddHistory msg)
    {
        if (!CheckSelected(ent, msg.Actor, out var mob, out var key))
            return;

        var line = msg.Line.Trim();
        if (line.Length < 1 || line.Length > ent.Comp.MaxStringLength)
            return;

        GetOfficer(mob.Value, out var officer);

        if (!_criminalRecords.TryAddHistory(key.Value, line, officer))
            return;

        // no radio message since its not crucial to officers patrolling

        UpdateUserInterface(ent);
    }

    private void OnDeleteHistory(Entity<CriminalRecordsConsoleComponent> ent, ref CriminalRecordDeleteHistory msg)
    {
        if (!CheckSelected(ent, msg.Actor, out _, out var key))
            return;

        if (!_criminalRecords.TryDeleteHistory(key.Value, msg.Index))
            return;

        // a bit sus but not crucial to officers patrolling
        UpdateUserInterface(ent);
    }

    // CorvaxGoob-SecurityFeatures
    private void OnPrint(Entity<CriminalRecordsConsoleComponent> ent, ref CriminalRecordPrint msg)
    {
        if (!CheckSelected(ent, msg.Actor, out _, out var key))
            return;

        if (ent.Comp.NextPrintTime > _timing.CurTime)
            return;

        if (!_criminalRecords.TryGetHistory(key.Value, msg.Index, out var crimeHistory))
            return;

        if (crimeHistory.Value.Status is null)
            return;

        if (!_records.TryGetRecord<GeneralStationRecord>(key.Value, out var entry))
            return;

        switch (crimeHistory.Value.Status.Value)
        {
            case SecurityStatus.Detained:
                PrintDetainedDocument(ent, msg.Actor, entry, crimeHistory.Value.Articles, crimeHistory.Value.Duration);
                break;
            case SecurityStatus.Wanted:
                PrintWantedDocument(ent, msg.Actor, entry);
                break;
            default:
                break;
        }

        ent.Comp.NextPrintTime = _timing.CurTime + ent.Comp.PrintCooldown;
        Dirty(ent);
    }

    private void UpdateUserInterface(Entity<CriminalRecordsConsoleComponent> ent)
    {
        var (uid, console) = ent;
        var owningStation = _station.GetOwningStation(uid);

        if (!TryComp<StationRecordsComponent>(owningStation, out var stationRecords))
        {
            _ui.SetUiState(uid, CriminalRecordsConsoleKey.Key, new CriminalRecordsConsoleState());
            return;
        }

        // get the listing of records to display
        var listing = _records.BuildListing((owningStation.Value, stationRecords), console.Filter);

        // filter the listing by the selected criminal record status
        //if NONE, dont filter by status, just show all crew
        if (console.FilterStatus != SecurityStatus.None)
        {
            listing = listing
                .Where(x => _records.TryGetRecord<CriminalRecord>(new StationRecordKey(x.Key, owningStation.Value), out var record) && record.Status == console.FilterStatus)
                .ToDictionary(x => x.Key, x => x.Value);
        }

        var state = new CriminalRecordsConsoleState(listing, console.Filter);
        if (console.ActiveKey is { } id)
        {
            // get records to display when a crewmember is selected
            var key = new StationRecordKey(id, owningStation.Value);
            _records.TryGetRecord(key, out state.StationRecord, stationRecords);
            _records.TryGetRecord(key, out state.CriminalRecord, stationRecords);
            state.SelectedKey = id;
        }

        // Set the Current Tab aka the filter status type for the records list
        state.FilterStatus = console.FilterStatus;

        _ui.SetUiState(uid, CriminalRecordsConsoleKey.Key, state);
    }

    /// <summary>
    /// Boilerplate that most actions use, if they require that a record be selected.
    /// Obviously shouldn't be used for selecting records.
    /// </summary>
    private bool CheckSelected(Entity<CriminalRecordsConsoleComponent> ent, EntityUid user,
        [NotNullWhen(true)] out EntityUid? mob, [NotNullWhen(true)] out StationRecordKey? key)
    {
        key = null;
        mob = null;
        if (!_access.IsAllowed(user, ent))
        {
            _popup.PopupEntity(Loc.GetString("criminal-records-permission-denied"), ent, user);
            return false;
        }

        if (ent.Comp.ActiveKey is not { } id)
            return false;

        // checking the console's station since the user might be off-grid using on-grid console
        if (_station.GetOwningStation(ent) is not { } station)
            return false;

        key = new StationRecordKey(id, station);
        mob = user;
        return true;
    }
}
