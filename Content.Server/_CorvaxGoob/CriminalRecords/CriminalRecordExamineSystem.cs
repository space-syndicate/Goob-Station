// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Diagnostics.CodeAnalysis;
using Content.Server.Station.Systems;
using Content.Server.StationRecords.Systems;
using Content.Shared.Access.Components;
using Content.Shared.CriminalRecords;
using Content.Shared.Examine;
using Content.Shared.Inventory;
using Content.Shared.Overlays;
using Content.Shared.Security;
using Content.Shared.StationRecords;
using Robust.Shared.Utility;

namespace Content.Server._CorvaxGoob.CriminalRecords;

// #criminal-record-examine
public sealed class CriminalRecordExamineSystem : EntitySystem
{
    [Dependency] private InventorySystem _inventory = default!;
    [Dependency] private StationRecordsSystem _records = default!;
    [Dependency] private StationSystem _station = default!;

    private const int ExaminePriority = -100;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<IdExaminableComponent, ExaminedEvent>(OnExamined);
    }

    private void OnExamined(Entity<IdExaminableComponent> ent, ref ExaminedEvent args)
    {
        // Keep this server-side: full criminal records are stored in station records, not on the examined mob.
        if (args.Examiner == ent.Owner || !HasSecurityHud(args.Examiner))
            return;

        if (!TryGetCriminalRecord(ent.Owner, out var record) ||
            record.Status == SecurityStatus.None)
            return;

        var status = Loc.GetString($"criminal-records-status-{record.Status.ToString().ToLowerInvariant()}");
        args.PushMessage(GetExamineMessage(record, status), ExaminePriority);
    }

    private bool HasSecurityHud(EntityUid user)
    {
        // Security cyber-eyes add this component directly to the body through Organ.onAdd.
        if (HasComp<ShowCriminalRecordIconsComponent>(user))
            return true;

        // Worn SecHUD glasses keep the component on the item in the eyes slot.
        return _inventory.TryGetSlotEntity(user, "eyes", out var eyes) &&
               HasComp<ShowCriminalRecordIconsComponent>(eyes.Value);
    }

    private bool TryGetCriminalRecord(
        EntityUid target,
        [NotNullWhen(true)] out CriminalRecord? record)
    {
        record = null;

        // Stop if the target has no owning station or that station has no records storage.
        if (_station.GetOwningStation(target) is not { } station ||
            !TryComp<StationRecordsComponent>(station, out var stationRecords))
            return false;

        // Match the existing glasses criminal records menu: the target record is resolved by station and entity name.
        var name = MetaData(target).EntityName;
        if (_records.GetRecordByName(station, name, stationRecords) is not { } id)
            return false;

        return _records.TryGetRecord<CriminalRecord>(
            new StationRecordKey(id, station),
            out record,
            stationRecords);
    }

    private static FormattedMessage GetExamineMessage(CriminalRecord record, string status)
    {
        var message = new FormattedMessage();
        var escapedStatus = FormattedMessage.EscapeText(status);

        // Add a plain blank line between regular examine text and criminal record details.
        message.PushNewline();

        // Color only the status itself; the player-provided description below keeps normal examine coloring.
        message.AddMarkupOrThrow($"[color={GetStatusColor(record.Status)}]{escapedStatus}[/color]");

        if (!string.IsNullOrWhiteSpace(record.Reason))
        {
            message.PushNewline();
            message.AddText($" - {FormattedMessage.EscapeText(record.Reason.Trim())}");
        }

        return message;
    }

    private static string GetStatusColor(SecurityStatus status)
    {
        return status switch
        {
            SecurityStatus.Suspected => "#33CCCC",
            SecurityStatus.Wanted => "#ff0000",
            SecurityStatus.Hostile => "#bf0909",
            SecurityStatus.Detained => "#B18644",
            SecurityStatus.Paroled => "#7FB717",
            SecurityStatus.Discharged => "#288EFF",
            SecurityStatus.Eliminated => "#FFFFFF",
            SecurityStatus.Search => "#33CCCC",
            SecurityStatus.Perma => "#f29430",
            SecurityStatus.Dangerous => "#8e0202",
            SecurityStatus.Demote => "#ed5146",
            _ => "#FFFFFF",
        };
    }
}
