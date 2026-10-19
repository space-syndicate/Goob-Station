using Robust.Shared.Prototypes;

namespace Content.Shared._Aquila.Economy;

[Prototype]
public sealed partial class VendingPriceListPrototype : IPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// Цены предметов в торгоматах. Цена базового прототипа действует на всех наследников, у которых нет своей цены.
    /// </summary>
    [DataField(required: true)]
    public Dictionary<EntProtoId, int> Prices = new();
}
