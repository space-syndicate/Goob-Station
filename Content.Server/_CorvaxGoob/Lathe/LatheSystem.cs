// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Collections.Generic;
using Content.Shared.Materials;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Server.Lathe;

public sealed partial class LatheSystem
{
    private static readonly ProtoId<MaterialPrototype> PlasteelMaterial = "Plasteel";
    private static readonly ProtoId<MaterialPrototype> WoodMaterial = "Wood";

    /// <summary>
    /// Adds explicit runtime material whitelist entries for department techfabs.
    /// </summary>
    /// <remarks>
    /// Material insertion is validated by both the entity whitelist from YAML and the runtime
    /// material whitelist generated for the lathe. The YAML whitelist filters inserted item
    /// stacks by tags. The runtime whitelist filters accepted material prototype ids.
    /// Engineering and cargo require explicit Plasteel/Wood entries here;
    /// ignoreMaterialWhiteList is not used because it disables the runtime material filter for all materials.
    /// </remarks>
    private void AddDepartmentFabricatorMaterials(EntityUid uid, List<ProtoId<MaterialPrototype>> materialWhitelist)
    {
        if (MetaData(uid).EntityPrototype?.ID is not ("EngineeringTechFab" or "CargoTechFab"))
            return;

        AddMaterialWhitelist(materialWhitelist, PlasteelMaterial);
        AddMaterialWhitelist(materialWhitelist, WoodMaterial);
    }

    // Recipe packs can already add the same material, so keep this idempotent.
    private static void AddMaterialWhitelist(List<ProtoId<MaterialPrototype>> materialWhitelist, ProtoId<MaterialPrototype> material)
    {
        if (!materialWhitelist.Contains(material))
            materialWhitelist.Add(material);
    }
}
