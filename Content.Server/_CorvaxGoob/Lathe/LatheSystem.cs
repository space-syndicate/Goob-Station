// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Collections.Generic;
using Content.Shared.Materials;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Server.Lathe;

public sealed partial class LatheSystem
{
    private static readonly ProtoId<MaterialPrototype>[] EngineeringTechFabMaterials =
    {
        "Plasteel",
        "ReinforcedGlass",
        "ReinforcedPlasmaGlass",
        "ReinforcedUraniumGlass",
        "Wood",
    };

    private static readonly ProtoId<MaterialPrototype>[] CargoTechFabMaterials =
    {
        "Durathread",
        "PlasmaGlass",
        "Plasteel",
        "ReinforcedGlass",
        "ReinforcedPlasmaGlass",
        "ReinforcedUraniumGlass",
        "UraniumGlass",
        "Wood",
    };

    /// <summary>
    /// Adds explicit runtime material whitelist entries for department techfabs.
    /// </summary>
    /// <remarks>
    /// Material insertion is validated by both the entity whitelist from YAML and the runtime material
    /// whitelist generated for the lathe. The YAML whitelist filters inserted item stacks by tags.
    /// The runtime whitelist filters accepted material prototype ids. Some materials must be added
    /// explicitly because no available recipe references them, so they would otherwise be hidden from
    /// the UI and rejected by insert/eject checks. ignoreMaterialWhiteList is not used because it
    /// disables the runtime material filter for all materials. The caller removes recipe-derived
    /// duplicates when it combines the material lists.
    /// </remarks>
    private void AddDepartmentFabricatorMaterials(EntityUid uid, List<ProtoId<MaterialPrototype>> materialWhitelist)
    {
        var additionalMaterials = MetaData(uid).EntityPrototype?.ID switch
        {
            "EngineeringTechFab" => EngineeringTechFabMaterials,
            "CargoTechFab" => CargoTechFabMaterials,
            _ => null,
        };

        if (additionalMaterials != null)
            materialWhitelist.AddRange(additionalMaterials);
    }
}
