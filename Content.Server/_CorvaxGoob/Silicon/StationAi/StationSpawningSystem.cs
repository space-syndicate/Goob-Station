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
    /// Assigns the initial station AI lawset, defaulting to NTDefault if loadout resolution fails.
    /// </summary>
    private void ApplySiliconLawLoadout(EntityUid entity, string roleLoadoutId, RoleLoadout? roleLoadout)
    {
        // This hook runs for every non-humanoid job entity, so it must not change other silicons such as borgs.
        if (roleLoadoutId != StationAiRoleLoadoutId ||
            !TryComp(entity, out SiliconLawProviderComponent? provider))
            return;

        // Apply the selected lawset, falling back to NTDefault if loadout resolution fails.
        provider.Laws = TryGetSelectedLawset(roleLoadout, out var lawsetId)
            ? lawsetId
            : DefaultStationAiLawset;

        // Invalidate the lawset cached during MapInit so the next GetLaws call resolves the assigned prototype.
        provider.Lawset = null;
    }

    /// <summary>
    /// Resolves the selected fixed lawset or samples its weighted table without modifying the entity.
    /// </summary>
    private bool TryGetSelectedLawset(RoleLoadout? roleLoadout, out ProtoId<SiliconLawsetPrototype> lawsetId)
    {
        lawsetId = default;

        if (roleLoadout == null ||
            !roleLoadout.SelectedLoadouts.TryGetValue(StationAiLawsetGroup, out var selectedLawsets) ||
            selectedLawsets.Count != 1)
            return false;

        var selected = selectedLawsets[0];
        if (!_prototypeManager.TryIndex(selected.Prototype, out LoadoutPrototype? loadout))
            return false;

        var lawset = loadout.SiliconLawset;

        if (loadout.RandomSiliconLawset is { } randomLawsetId)
        {
            if (!_prototypeManager.TryIndex(randomLawsetId, out WeightedRandomPrototype? randomLawsets))
                return false;

            if (randomLawsets.Weights.Count == 0 || randomLawsets.Weights.Values.Any(weight => weight <= 0f))
                return false;

            lawset = randomLawsets.Pick(_random);
        }

        if (lawset is not { } resolvedLawsetId || !_prototypeManager.HasIndex(resolvedLawsetId))
            return false;

        lawsetId = resolvedLawsetId;
        return true;
    }
}
