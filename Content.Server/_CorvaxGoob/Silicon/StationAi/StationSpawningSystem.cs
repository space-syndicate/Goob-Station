// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Shared.Preferences.Loadouts;
using Content.Shared.Random;
using Content.Shared.Random.Helpers;
using Content.Shared.Silicons.Laws;
using Content.Shared.Silicons.Laws.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.Station.Systems;

public sealed partial class StationSpawningSystem
{
    private const string StationAiRoleLoadoutId = "JobStationAi";
    private static readonly ProtoId<LoadoutGroupPrototype> StationAiLawsetGroup = "StationAiLaws";
    private static readonly ProtoId<SiliconLawsetPrototype> DefaultStationAiLawset = "NTDefault";

    [Dependency] private IRobustRandom _random = default!;

    /// <summary>
    /// Applies the fixed or random lawset selected in the station AI's role loadout.
    /// Falls back to NTDefault when no valid lawset can be applied.
    /// </summary>
    private void ApplySiliconLawLoadout(EntityUid entity, string roleLoadoutId, RoleLoadout? roleLoadout)
    {
        // This hook runs for every non-humanoid job entity, so it must not change other silicons such as borgs.
        if (roleLoadoutId != StationAiRoleLoadoutId ||
            !TryComp(entity, out SiliconLawProviderComponent? provider))
            return;

        // Set the safe default first so missing, invalid, or outdated loadout data cannot leave the AI without laws.
        provider.Laws = DefaultStationAiLawset;
        provider.Lawset = null;

        if (roleLoadout == null ||
            !roleLoadout.SelectedLoadouts.TryGetValue(StationAiLawsetGroup, out var selectedLawsets) ||
            selectedLawsets.Count != 1)
            return;

        var selected = selectedLawsets[0];
        if (!_prototypeManager.TryIndex(selected.Prototype, out LoadoutPrototype? loadout))
        {
            Log.Error($"Unable to find station AI lawset loadout {selected.Prototype}");
            return;
        }

        var lawset = loadout.SiliconLawset;

        if (loadout.RandomSiliconLawset is { } randomLawsetId)
        {
            // Choose the random initial lawset once when this silicon is spawned.
            if (!_prototypeManager.TryIndex(randomLawsetId, out WeightedRandomPrototype? randomLawsets))
            {
                Log.Error($"Unable to find silicon lawset table {randomLawsetId} for loadout {loadout.ID}");
                return;
            }

            if (randomLawsets.Weights.Count == 0 || randomLawsets.Weights.Values.Any(weight => weight <= 0f))
            {
                Log.Error($"Silicon lawset table {randomLawsetId} for loadout {loadout.ID} has invalid weights");
                return;
            }

            lawset = randomLawsets.Pick(_random);
        }

        if (lawset is not { } lawsetId)
        {
            Log.Error($"Station AI lawset loadout {loadout.ID} does not specify a lawset");
            return;
        }

        if (!_prototypeManager.HasIndex(lawsetId))
        {
            Log.Error($"Unable to find silicon lawset {lawsetId} for loadout {loadout.ID}");
            return;
        }

        // MapInit may have cached the entity prototype's default laws before the loadout was applied.
        provider.Laws = lawsetId;
        provider.Lawset = null;
    }
}
