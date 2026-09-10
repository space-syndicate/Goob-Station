// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.CrewManifest;
using Content.Server.GameTicking;
using Content.Server.Mind;
using Content.Server.Roles.Jobs;
using Content.Server.Station.Systems;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Inventory;
using Content.Shared.Paper;
using Content.Shared.PDA;
using Robust.Server.GameObjects;
using Robust.Shared.Timing;
using static Content.Shared.Paper.PaperComponent;

namespace Content.Server.Paper;

/// <summary>
/// Provides data for the paper insert helper.
/// Checks the request on the server and sends the response only to the requesting player.
/// </summary>
public sealed partial class PaperInsertDataSystem : EntitySystem
{
    private const string IdSlot = "id";

    /// Limits helper text and manifest size to prevent oversized UI payloads.
    private const int MaxInsertTextLength = 256;
    private const int MaxManifestEntries = 256;

    [Dependency] private CrewManifestSystem _crewManifest = default!;
    [Dependency] private GameTicker _gameTicker = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private InventorySystem _inventory = default!;
    [Dependency] private JobSystem _jobs = default!;
    [Dependency] private MindSystem _mind = default!;
    [Dependency] private StationSystem _station = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private UserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PaperComponent, PaperInsertDataRequestMessage>(OnInsertDataRequest);
    }

    private void OnInsertDataRequest(Entity<PaperComponent> ent, ref PaperInsertDataRequestMessage args)
    {
        if (!CanSendInsertData(ent.Comp, args.UiKey))
            return;

        SendInsertData(ent, args.Actor);
    }

    private static bool CanSendInsertData(PaperComponent paper, object? uiKey)
    {
        return Equals(uiKey, PaperUiKey.Key) && paper.Mode == PaperAction.Write;
    }

    private void SendInsertData(Entity<PaperComponent> ent, EntityUid actor)
    {
        // Send player-specific helper data only to the requesting player.
        // Shared BUI state would send it to everyone viewing the paper.
        _ui.ServerSendUiMessage(ent.Owner, PaperUiKey.Key, BuildInsertData(actor), actor);
    }

    private PaperInsertDataMessage BuildInsertData(EntityUid actor)
    {
        var serverNow = DateTime.Now;
        var manifestEntries = Array.Empty<PaperInsertManifestEntry>();
        string? stationName = null;
        var ownName = SanitizeOptionalInsertText(Name(actor));
        string? ownJob = null;

        if (_mind.TryGetMind(actor, out var mindId, out _) &&
            _jobs.MindTryGetJobName(mindId, out var jobName))
            ownJob = SanitizeOptionalInsertText(jobName);

        var station = _station.GetOwningStation(actor);
        if (station is { } stationUid)
        {
            stationName = SanitizeOptionalInsertText(Name(stationUid));

            // Station, name, and job come from the character.
            // Manifest entries require a PDA in the ID slot or in hand.
            if (HasAccessiblePda(actor))
                manifestEntries = BuildManifestEntries(stationUid);
        }

        return CreateMessage(stationName, ownName, ownJob, manifestEntries, serverNow);
    }

    // PDAs inside storage do not grant manifest access.
    // Only a PDA worn in the ID slot or held in hand is accepted.
    private bool HasAccessiblePda(EntityUid actor)
    {
        if (_inventory.TryGetSlotEntity(actor, IdSlot, out var maybePda) &&
            maybePda is { } pdaUid &&
            HasComp<PdaComponent>(pdaUid))
        {
            return true;
        }

        foreach (var held in _hands.EnumerateHeld(actor))
        {
            if (HasComp<PdaComponent>(held))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Builds a limited manifest list for the station, cleans its text, and sorts entries by job title.
    /// </summary>
    private PaperInsertManifestEntry[] BuildManifestEntries(EntityUid station)
    {
        var (_, manifest) = _crewManifest.GetCrewManifest(station);
        if (manifest == null)
            return Array.Empty<PaperInsertManifestEntry>();

        var count = Math.Min(manifest.Entries.Length, MaxManifestEntries);
        var entries = new PaperInsertManifestEntry[count];

        for (var i = 0; i < count; i++)
        {
            var entry = manifest.Entries[i];
            entries[i] = new PaperInsertManifestEntry(
                SanitizeInsertText(entry.Name),
                SanitizeInsertText(entry.JobTitle));
        }

        // The dropdown is a quick insert tool, not the full manifest UI. Sort by job title first
        // so users can scan for a role alphabetically; names are only a stable tie-breaker.
        Array.Sort(entries, (a, b) =>
        {
            var jobComparison = string.Compare(a.JobTitle, b.JobTitle, StringComparison.CurrentCultureIgnoreCase);
            if (jobComparison != 0)
                return jobComparison;

            return string.Compare(a.Name, b.Name, StringComparison.CurrentCultureIgnoreCase);
        });

        return entries;
    }

    private PaperInsertDataMessage CreateMessage(
        string? stationName,
        string? ownName,
        string? ownJob,
        PaperInsertManifestEntry[] manifestEntries,
        DateTime serverNow)
    {
        return new PaperInsertDataMessage(
            stationName,
            ownName,
            ownJob,
            manifestEntries,
            _gameTicker.RoundDuration(),
            _timing.CurTime,
            serverNow.Day,
            serverNow.Month,
            serverNow.Year);
    }

    private static string SanitizeInsertText(string value)
    {
        return SanitizeOptionalInsertText(value) ?? string.Empty;
    }

    private static string? SanitizeOptionalInsertText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        // Keep insert values single-line. This prevents a hostile/custom manifest value from
        // reshaping the helper dropdown while still preserving the readable text users expect.
        value = value.Replace('\r', ' ').Replace('\n', ' ').Trim();

        if (value.Length > MaxInsertTextLength)
            value = value[..MaxInsertTextLength];

        return value;
    }
}
