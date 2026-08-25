// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Collections.Generic;
using Content.Shared.Materials;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Server.Lathe;

public sealed partial class LatheSystem
{
    private static readonly ProtoId<MaterialPrototype> DurathreadMaterial = "Durathread";
    private static readonly ProtoId<MaterialPrototype> PlasmaGlassMaterial = "PlasmaGlass";
    private static readonly ProtoId<MaterialPrototype> PlasteelMaterial = "Plasteel";
    private static readonly ProtoId<MaterialPrototype> ReinforcedGlassMaterial = "ReinforcedGlass";
    private static readonly ProtoId<MaterialPrototype> ReinforcedPlasmaGlassMaterial = "ReinforcedPlasmaGlass";
    private static readonly ProtoId<MaterialPrototype> ReinforcedUraniumGlassMaterial = "ReinforcedUraniumGlass";
    private static readonly ProtoId<MaterialPrototype> UraniumGlassMaterial = "UraniumGlass";
    private static readonly ProtoId<MaterialPrototype> WoodMaterial = "Wood";

    /// <summary>
    /// Adds explicit runtime material whitelist entries for department techfabs.
    /// </summary>
    /// <remarks>
    /// Material insertion is validated by both the entity whitelist from YAML and the runtime material
    /// whitelist generated for the lathe. The YAML whitelist filters inserted item stacks by tags.
    /// The runtime whitelist filters accepted material prototype ids. Some materials must be added
    /// explicitly because no available recipe references them, so they would otherwise be hidden from
    /// the UI and rejected by insert/eject checks. ignoreMaterialWhiteList is not used because it
    /// disables the runtime material filter for all materials.
    /// </remarks>
    private void AddDepartmentFabricatorMaterials(EntityUid uid, List<ProtoId<MaterialPrototype>> materialWhitelist)
    {
        switch (MetaData(uid).EntityPrototype?.ID)
        {
            case "EngineeringTechFab":
                AddEngineeringTechFabMaterials(materialWhitelist);
                break;
            case "CargoTechFab":
                AddCargoTechFabMaterials(materialWhitelist);
                break;
        }
    }

    private static void AddEngineeringTechFabMaterials(List<ProtoId<MaterialPrototype>> materialWhitelist)
    {
        AddMaterialWhitelist(materialWhitelist, PlasteelMaterial);
        AddMaterialWhitelist(materialWhitelist, ReinforcedGlassMaterial);
        AddMaterialWhitelist(materialWhitelist, ReinforcedPlasmaGlassMaterial);
        AddMaterialWhitelist(materialWhitelist, ReinforcedUraniumGlassMaterial);
        AddMaterialWhitelist(materialWhitelist, WoodMaterial);
    }

    private static void AddCargoTechFabMaterials(List<ProtoId<MaterialPrototype>> materialWhitelist)
    {
        AddMaterialWhitelist(materialWhitelist, DurathreadMaterial);
        AddMaterialWhitelist(materialWhitelist, PlasmaGlassMaterial);
        AddMaterialWhitelist(materialWhitelist, PlasteelMaterial);
        AddMaterialWhitelist(materialWhitelist, ReinforcedGlassMaterial);
        AddMaterialWhitelist(materialWhitelist, ReinforcedPlasmaGlassMaterial);
        AddMaterialWhitelist(materialWhitelist, ReinforcedUraniumGlassMaterial);
        AddMaterialWhitelist(materialWhitelist, UraniumGlassMaterial);
        AddMaterialWhitelist(materialWhitelist, WoodMaterial);
    }

    // Recipe packs can already add the same material, so keep this idempotent.
    private static void AddMaterialWhitelist(List<ProtoId<MaterialPrototype>> materialWhitelist, ProtoId<MaterialPrototype> material)
    {
        if (!materialWhitelist.Contains(material))
            materialWhitelist.Add(material);
    }
}
