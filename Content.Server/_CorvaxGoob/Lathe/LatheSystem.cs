// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Materials;
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
    /// Adds department material ids that are not discovered from recipes.
    /// </summary>
    /// <remarks>
    /// Keeps extra engineering and cargo materials visible and insertable without disabling the
    /// runtime material whitelist for every material.
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
