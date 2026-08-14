// SPDX-License-Identifier: AGPL-3.0-or-later

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
    [Dependency] private IRobustRandom _random = default!;

    /// <summary>
    /// Applies the fixed or random lawset selected in a silicon's role loadout.
    /// </summary>
    private void ApplySiliconLawLoadout(EntityUid entity, RoleLoadout? roleLoadout)
    {
        // This hook runs for every non-humanoid job entity, most of which do not provide silicon laws.
        if (roleLoadout == null || !TryComp(entity, out SiliconLawProviderComponent? provider))
            return;

        // A role loadout can contain several groups, but only the lawset group uses these fields.
        foreach (var group in roleLoadout.SelectedLoadouts.Values)
        {
            foreach (var selected in group)
            {
                if (!_prototypeManager.TryIndex(selected.Prototype, out LoadoutPrototype? loadout))
                    continue;

                var lawset = loadout.SiliconLawset;

                if (loadout.RandomSiliconLawset is { } randomLawsetId)
                {
                    // Choose the random initial lawset once when this silicon is spawned.
                    if (!_prototypeManager.TryIndex(randomLawsetId, out WeightedRandomPrototype? randomLawsets))
                    {
                        Log.Error($"Unable to find silicon lawset table {randomLawsetId} for loadout {loadout.ID}");
                        continue;
                    }

                    lawset = randomLawsets.Pick(_random);
                }

                if (lawset is not { } lawsetId)
                    continue;

                if (!_prototypeManager.HasIndex(lawsetId))
                {
                    Log.Error($"Unable to find silicon lawset {lawsetId} for loadout {loadout.ID}");
                    continue;
                }

                // MapInit may have cached the entity prototype's default laws before the loadout was applied.
                provider.Laws = lawsetId;
                provider.Lawset = null;
                return;
            }
        }
    }
}
