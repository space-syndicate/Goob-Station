// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Random;
using Content.Shared.Silicons.Laws;
using Robust.Shared.Prototypes;

namespace Content.Shared.Preferences.Loadouts;

public sealed partial class LoadoutPrototype
{
    /// <summary>
    /// A fixed silicon lawset applied by this loadout.
    /// </summary>
    [DataField]
    public ProtoId<SiliconLawsetPrototype>? SiliconLawset;

    /// <summary>
    /// A weighted lawset table used to choose the silicon's initial laws.
    /// </summary>
    [DataField]
    public ProtoId<WeightedRandomPrototype>? RandomSiliconLawset;
}
