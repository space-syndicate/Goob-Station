// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Security;
using Content.Shared.StationRecords;
using Robust.Shared.Utility;

namespace Content.Server.CriminalRecords.Systems;

public sealed partial class CriminalRecordsConsoleSystem
{
    private const string StatusChangeRadioColor = "#FFA726";
    private const int MaxDetainedDurationMinutes = 1440;

    // Formats security status change notifications sent to the security radio channel by the criminal records console and SecHUD.
    // Final format examples:
    // [Detained] David Norty (Head of Personnel), reason: 123, officer: La-Riker (Captain).
    // [None] David Norty (Head of Personnel), officer: La-Riker (Captain).
    private string FormatStatusChangeRadioMessage(
        string statusString,
        string name,
        string officer,
        string job,
        string? reason = null)
    {
        var status = Loc.GetString($"criminal-records-status-{statusString}");
        var locId = reason == null
            ? "criminal-records-console-radio-status"
            : "criminal-records-console-radio-status-with-reason";
        var reasonText = reason ?? Loc.GetString("criminal-records-console-unspecified");
        var message = Loc.GetString(locId, GetStatusChangeRadioArgs(status, name, officer, job, reasonText));

        return ColorStatusChangeRadioMessage(message);
    }

    // Adds the outer color markup after all user-provided values have been escaped.
    private static string ColorStatusChangeRadioMessage(string message)
    {
        return $"[color={StatusChangeRadioColor}]{message}[/color]";
    }

    // Name and job are split by the ftl pattern, so the bold markup starts in name and ends in job.
    private static (string, object)[] GetStatusChangeRadioArgs(
        string status,
        string name,
        string officer,
        string job,
        string reason)
    {
        return new (string, object)[]
        {
            ("status", FormattedMessage.EscapeText(status)),
            ("name", $"[bold]{FormattedMessage.EscapeText(name)}"),
            ("job", $"{FormattedMessage.EscapeText(job)}[/bold]"),
            ("reason", FormattedMessage.EscapeText(reason)),
            ("officer", FormattedMessage.EscapeText(officer)),
        };
    }

    private static bool IsValidDetainedDuration(int? duration)
    {
        return duration is > 0 and <= MaxDetainedDurationMinutes;
    }

    private void TryAddSecHudStatusHistory(
        StationRecordKey key,
        SecurityStatus status,
        string statusString,
        string? reason,
        string officer)
    {
        // Detained has its own history entry with articles and sentence duration.
        if (status == SecurityStatus.Detained)
            return;

        _criminalRecords.TryAddHistory(key, Loc.GetString("criminal-records-console-history",
            ("status", Loc.GetString($"criminal-records-status-{statusString}")),
            ("reason", reason ?? Loc.GetString("criminal-records-console-unspecified"))),
            officer,
            articles: status == SecurityStatus.Wanted ? reason : null,
            status: status);
    }
}
