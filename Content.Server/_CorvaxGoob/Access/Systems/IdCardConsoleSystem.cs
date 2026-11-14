// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server.Popups;
using Content.Shared.Access;
using Content.Shared.Access.Components;
using static Content.Shared.Access.Components.IdCardConsoleComponent;
using Content.Shared.Database;
using Content.Shared.Roles;
using Content.Shared.StationRecords;
using Robust.Shared.Prototypes;

namespace Content.Server.Access.Systems;

public sealed partial class IdCardConsoleSystem
{
    private static readonly ProtoId<JobPrototype> HeadOfPersonnelJob = "HeadOfPersonnel";

    private static readonly HashSet<ProtoId<AccessLevelPrototype>> ExtendedAccessExclusions =
    [
        "Armory",
        "Captain",
        "ChiefMedicalOfficer",
        "HeadOfPersonnel",
        "ResearchDirector",
        "HeadOfSecurity",
        "Quartermaster",
        "ChiefEngineer",
        "NanotrasenRepresentative",
        "BlueshieldOfficer",
        "CentralCommand",
        "GenpopEnter",
        "GenpopLeave",
    ];

    // These are title-text exceptions, not job prototype checks: custom titles stay marked.
    private static readonly string[] CaptainJobTitles =
    [
        "капитан",
        "Captain",
    ];

    private static readonly string[] HeadOfPersonnelJobTitles =
    [
        "глава персонала",
        "Head of Personnel",
    ];

    [Dependency] private PopupSystem _popup = default!;

    /// <summary>
    /// Registers the isolated extended-access message handler for the ID console.
    /// </summary>
    private void InitializeExtendedAccess()
    {
        SubscribeLocalEvent<IdCardConsoleComponent, IdCardConsoleExtendedAccessMessage>(OnExtendedAccessMessage);
    }

    private void OnExtendedAccessMessage(EntityUid uid, IdCardConsoleComponent component, IdCardConsoleExtendedAccessMessage args)
    {
        if (args.Actor is not { Valid: true } player)
            return;

        TryApplyExtendedAccessAction(uid, args.Action, player, component);

        UpdateUserInterface(uid, component, args);
    }

    private void TryApplyExtendedAccessAction(
        EntityUid uid,
        IdCardConsoleExtendedAccessAction action,
        EntityUid player,
        IdCardConsoleComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        // Extended-access actions only run when both ID cards are present, the privileged card is authorized,
        // and the target slot contains an ID card component that can be modified.
        if (component.TargetIdSlot.Item is not { Valid: true } targetId
            || !PrivilegedIdIsAuthorized(uid, component, out var privilegedId)
            || !TryComp<IdCardComponent>(targetId, out var targetIdComponent))
        {
            return;
        }

        // Collect the relevant extended-access state: the privileged card's access (`privilegedTags`),
        // the access shown on this console (`visibleTags`), the access that can actually be modified
        // (`modifiableTags`), the target card's current access (`oldTags`), and the placeholders for
        // the updated access/job result (`newTags`, `newJob`, `newJobTitle`, `changedIdentity`).
        var privilegedTags = _accessReader.FindAccessTags(privilegedId.Value).ToHashSet();
        var visibleTags = component.AccessLevels.ToHashSet();
        var modifiableTags = privilegedTags.Intersect(visibleTags).ToHashSet();
        var oldTags = (_access.TryGetTags(targetId) ?? Array.Empty<ProtoId<AccessLevelPrototype>>()).ToHashSet();
        HashSet<ProtoId<AccessLevelPrototype>> newTags;
        JobPrototype? newJob = null;
        var newJobTitle = targetIdComponent.LocalizedJobTitle ?? string.Empty;
        var changedIdentity = false;

        switch (action)
        {
            case IdCardConsoleExtendedAccessAction.StandardAccess:
                if (!TryResolveJobFromTitle(targetIdComponent.LocalizedJobTitle, out var resetJob))
                {
                    ShowResetFailed(uid, player);
                    return;
                }

                newJob = resetJob;
                newJobTitle = resetJob.LocalizedName;
                var resetJobAccess = GetJobAccessTags(resetJob).Intersect(modifiableTags);
                newTags = oldTags.Except(modifiableTags).Union(resetJobAccess).ToHashSet();
                changedIdentity = ApplyJobIdentity(targetId, targetIdComponent, resetJob, resetJob.LocalizedName, player);
                break;

            case IdCardConsoleExtendedAccessAction.Extended:
                newTags = oldTags.Union(GetExtendedAccessTags(modifiableTags)).ToHashSet();
                changedIdentity = ShouldSkipExtendedAccessMarker(targetIdComponent, oldTags)
                    ? false
                    : TrySetAccessMarker(targetId, targetIdComponent, true, player);
                break;

            default:
                return;
        }

        if (oldTags.SetEquals(newTags) && !changedIdentity)
            return;

        var changedAccess = !oldTags.SetEquals(newTags);
        if (changedAccess)
        {
            _access.TrySetTags(targetId, newTags);
            LogExtendedAccessChange(player, targetId, oldTags, newTags);
        }

        if (newJob != null)
            UpdateExtendedAccessStationRecord(targetId, targetIdComponent.FullName ?? string.Empty, newJobTitle, newJob);
        else if (changedIdentity)
            UpdateStationRecordJobTitle(targetId, targetIdComponent.LocalizedJobTitle ?? string.Empty);
    }

    private static HashSet<ProtoId<AccessLevelPrototype>> GetExtendedAccessTags(HashSet<ProtoId<AccessLevelPrototype>> modifiableTags)
    {
        // Extended access starts from tags the privileged ID can actually modify, then removes head/armory exceptions.
        return modifiableTags.Except(ExtendedAccessExclusions).ToHashSet();
    }

    private HashSet<ProtoId<AccessLevelPrototype>> GetJobAccessTags(JobPrototype job)
    {
        var tags = job.Access.ToHashSet();

        foreach (var group in job.AccessGroups)
        {
            if (!_prototype.TryIndex(group, out AccessGroupPrototype? groupPrototype))
                continue;

            tags.UnionWith(groupPrototype.Tags);
        }

        return tags;
    }

    private void LogExtendedAccessChange(
        EntityUid player,
        EntityUid targetId,
        HashSet<ProtoId<AccessLevelPrototype>> oldTags,
        HashSet<ProtoId<AccessLevelPrototype>> newTags)
    {
        var addedTags = newTags.Except(oldTags).Select(tag => "+" + tag).ToList();
        var removedTags = oldTags.Except(newTags).Select(tag => "-" + tag).ToList();

        // Keep the audit entry format aligned with normal ID console access writes.
        _adminLogger.Add(LogType.Action,
            $"{player} has modified {targetId} with the following accesses: [{string.Join(", ", addedTags.Union(removedTags))}] [{string.Join(", ", newTags)}]");
    }

    private bool ShouldSkipExtendedAccessMarker(IdCardComponent targetIdComponent, HashSet<ProtoId<AccessLevelPrototype>> oldTags)
    {
        if (MatchesSpecialJobTitle(targetIdComponent.LocalizedJobTitle, CaptainJobTitles))
            return true;

        if (!MatchesSpecialJobTitle(targetIdComponent.LocalizedJobTitle, HeadOfPersonnelJobTitles)
            || !_prototype.TryIndex(HeadOfPersonnelJob, out var headOfPersonnelJob))
        {
            return false;
        }

        return GetJobAccessTags(headOfPersonnelJob).IsSubsetOf(oldTags);
    }

    private static bool MatchesSpecialJobTitle(string? jobTitle, string[] expectedTitles)
    {
        var normalizedTitle = NormalizeJobTitle(jobTitle);
        // Marker skipping is intentionally exact: custom titles like "Navy Captain" still get marked.
        return expectedTitles.Any(title =>
            string.Equals(normalizedTitle, title, StringComparison.OrdinalIgnoreCase));
    }

    private bool TrySetAccessMarker(EntityUid targetId, IdCardComponent targetIdComponent, bool marked, EntityUid player)
    {
        var jobTitle = targetIdComponent.LocalizedJobTitle ?? string.Empty;
        var trimmedTitle = jobTitle.TrimEnd();
        var alreadyMarked = trimmedTitle.EndsWith('+');

        // The marker is visual only: repeat presses must not stack or keep removing title characters.
        if (alreadyMarked == marked)
            return false;

        var oldJobTitle = targetIdComponent.LocalizedJobTitle;
        var newJobTitle = marked
            ? string.IsNullOrWhiteSpace(trimmedTitle)
                ? "+"
                : $"{trimmedTitle}+"
            : trimmedTitle[..^1].TrimEnd();

        _idCard.TryChangeJobTitle(targetId, newJobTitle, targetIdComponent, player: player);
        return !string.Equals(oldJobTitle, targetIdComponent.LocalizedJobTitle, StringComparison.CurrentCulture);
    }

    private bool ApplyJobIdentity(
        EntityUid targetId,
        IdCardComponent targetIdComponent,
        JobPrototype job,
        string jobTitle,
        EntityUid player)
    {
        var changed = false;

        // Skip when the card already has this exact localized title.
        if (!string.Equals(targetIdComponent.LocalizedJobTitle, jobTitle, StringComparison.CurrentCulture))
        {
            _idCard.TryChangeJobTitle(targetId, jobTitle, targetIdComponent, player: player);
            changed = true;
        }

        if (_prototype.TryIndex(job.Icon, out var jobIcon))
        {
            if (targetIdComponent.JobIcon != jobIcon.ID)
            {
                _idCard.TryChangeJobIcon(targetId, jobIcon, targetIdComponent, player: player);
                changed = true;
            }

            var departments = _prototype
                .EnumeratePrototypes<DepartmentPrototype>()
                .Where(department => department.Roles.Contains(job.ID))
                .Select(department => new ProtoId<DepartmentPrototype>(department.ID))
                .ToHashSet();

            if (!targetIdComponent.JobDepartments.ToHashSet().SetEquals(departments))
            {
                _idCard.TryChangeJobDepartment(targetId, job, targetIdComponent);
                changed = true;
            }
        }

        if (targetIdComponent.JobPrototype != job.ID)
        {
            targetIdComponent.JobPrototype = job.ID;
            Dirty(targetId, targetIdComponent);
            changed = true;
        }

        return changed;
    }

    /// <summary>
    /// Tries to recover a visible console job from the localized title stored on the ID card.
    /// </summary>
    /// <remarks>
    /// The lookup first normalizes the card title and searches for a single exact localized-name match.
    /// If no exact match exists, it falls back to a single unambiguous case-insensitive "title contains full job name" match.
    /// Hidden console jobs are ignored, and ambiguous or missing matches fail safely.
    /// </remarks>
    private bool TryResolveJobFromTitle(string? jobTitle, [NotNullWhen(true)] out JobPrototype? job)
    {
        job = null;
        var normalizedTitle = NormalizeJobTitle(jobTitle);
        if (string.IsNullOrWhiteSpace(normalizedTitle))
            return false;

        var jobs = _prototype
            .EnumeratePrototypes<JobPrototype>()
            .Where(x => x.OverrideConsoleVisibility.GetValueOrDefault(x.SetPreference))
            .ToList();

        // Standard access recovery uses the localized card title, first exactly and then by one unambiguous contained job name.
        var exactMatches = jobs
            .Where(x => string.Equals(NormalizeJobTitle(x.LocalizedName), normalizedTitle, StringComparison.CurrentCultureIgnoreCase))
            .ToList();

        if (exactMatches.Count == 1)
        {
            job = exactMatches[0];
            return true;
        }

        if (exactMatches.Count > 1)
            return false;

        var containedMatches = jobs
            .Where(x =>
            {
                var localizedName = NormalizeJobTitle(x.LocalizedName);
                return !string.IsNullOrWhiteSpace(localizedName)
                    && normalizedTitle.Contains(localizedName, StringComparison.CurrentCultureIgnoreCase);
            })
            .ToList();

        if (containedMatches.Count != 1)
            return false;

        job = containedMatches[0];
        return true;
    }

    private static string NormalizeJobTitle(string? jobTitle)
    {
        var normalized = jobTitle?.Trim() ?? string.Empty;
        if (normalized.EndsWith('+'))
            normalized = normalized[..^1].Trim();

        return normalized;
    }

    private void ShowResetFailed(EntityUid uid, EntityUid player)
    {
        _popup.PopupEntity(Loc.GetString("id-card-console-reset-job-failed"), uid, player);
    }

    private void UpdateExtendedAccessStationRecord(
        EntityUid targetId,
        string newFullName,
        string newJobTitle,
        JobPrototype newJobProto)
    {
        if (!TryComp<StationRecordKeyStorageComponent>(targetId, out var keyStorage)
            || keyStorage.Key is not { } key
            || !_record.TryGetRecord<GeneralStationRecord>(key, out var record))
        {
            return;
        }

        record.Name = newFullName;
        record.JobTitle = newJobTitle;
        record.JobPrototype = newJobProto.ID;
        record.JobIcon = newJobProto.Icon;
        _record.Synchronize(key);
    }

    private void UpdateStationRecordJobTitle(EntityUid targetId, string newJobTitle)
    {
        if (!TryComp<StationRecordKeyStorageComponent>(targetId, out var keyStorage)
            || keyStorage.Key is not { } key
            || !_record.TryGetRecord<GeneralStationRecord>(key, out var record))
        {
            return;
        }

        record.JobTitle = newJobTitle;
        _record.Synchronize(key);
    }
}
