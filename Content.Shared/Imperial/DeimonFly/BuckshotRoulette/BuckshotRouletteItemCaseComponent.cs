using Robust.Shared.Prototypes;

namespace Content.Shared.Imperial.DeimonFly.BuckshotRoulette;

/// <summary>
/// Заполняет кейс случайными предметами Buckshot Roulette.
/// </summary>
[RegisterComponent]
public sealed partial class BuckshotRouletteItemCaseComponent : Component
{
    [DataField(required: true)]
    public List<EntProtoId> Items = new();

    [DataField]
    public int MinimumItems = 2;

    [DataField]
    public int MaximumItems = 8;
}
