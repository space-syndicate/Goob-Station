// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Security;
using Content.Shared.StationRecords;
using Robust.Shared.Utility;

namespace Content.Server.CriminalRecords.Systems;

public sealed partial class CriminalRecordsConsoleSystem
{
    private const string StatusChangeRadioColor = "#FF0D0D";
    private const int MaxDetainedDurationMinutes = 1440;

    private static string ColorStatusChangeRadioMessage(string message)
    {
        return $"[color={StatusChangeRadioColor}]{FormattedMessage.EscapeText(message)}[/color]";
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
