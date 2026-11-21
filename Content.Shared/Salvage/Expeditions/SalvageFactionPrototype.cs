// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Procedural; // CorvaxGoob
using Robust.Shared.Prototypes;

namespace Content.Shared.Salvage.Expeditions;

[Prototype]
public sealed partial class SalvageFactionPrototype : IPrototype
{
    [IdDataField] public string ID { get; private set; } = default!;

    /// Allowed difficulties. null — all difficulties are available. Added for CorvaxGoob.
    [DataField]
    public List<ProtoId<SalvageDifficultyPrototype>>? AllowedDifficulties;

    [DataField("desc")] public LocId Description { get; private set; } = string.Empty;

    [ViewVariables(VVAccess.ReadWrite), DataField("entries", required: true)]
    public List<SalvageMobEntry> MobGroups = new();

    /// <summary>
    /// Miscellaneous data for factions.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("configs")]
    public Dictionary<string, string> Configs = new();
}