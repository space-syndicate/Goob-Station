using Content.Shared.Atmos.EntitySystems;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Prototypes;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Localizations;
using Robust.Shared.Prototypes;

namespace Content.Shared._CorvaxGoob.Botany.PlantAnalyzer;

public sealed class PlantAnalyzerLocalizationHelper : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly SharedAtmosphereSystem _atmo = default!;

    public string GasesToLocalizedStrings(List<Gas> gases)
    {
        if (gases.Count == 0)
            return "";

        List<int> gasIds = [];
        foreach (var gas in gases)
            gasIds.Add((int)gas);

        List<string> gasesLoc = [];
        for (var i = 0; i < gasIds.Count; i++)
            gasesLoc.Add(Loc.GetString(_atmo.GetGas(gasIds[i]).Name));

        return ContentLocalizationManager.FormatListLocalized(gasesLoc, "plant-analyzer-and");
    }

    public string ChemicalsToLocalizedStrings(List<string> ids)
    {
        if (ids.Count == 0)
            return "";

        List<string> locStrings = [];
        foreach (var id in ids)
            locStrings.Add(_prototypeManager.TryIndex<ReagentPrototype>(id, out var prototype) ? prototype.LocalizedName : id);

        return ContentLocalizationManager.FormatListLocalized(locStrings, "plant-analyzer-and");
    }

    public (string Singular, string Plural, string First) ProduceToLocalizedStrings(List<EntProtoId> ids)
    {
        if (ids.Count == 0)
            return ("", "", "");

        List<string> singularStrings = [];
        List<string> pluralStrings = [];
        foreach (var id in ids)
        {
            var singular = _prototypeManager.TryIndex(id, out var prototype) ? prototype.Name : id.Id;
            var plural = Loc.GetString("plant-analyzer-produce-plural", ("thing", singular));

            singularStrings.Add(singular);
            pluralStrings.Add(plural);
        }

        return (
            ContentLocalizationManager.FormatListToOr(singularStrings),
            ContentLocalizationManager.FormatListToOr(pluralStrings),
            singularStrings[0]
        );
    }
}
