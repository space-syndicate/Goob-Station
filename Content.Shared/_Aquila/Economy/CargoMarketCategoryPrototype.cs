using Content.Shared.Tag;
using Robust.Shared.Prototypes;

namespace Content.Shared._Aquila.Economy;

[Prototype]
public sealed partial class CargoMarketCategoryPrototype : IPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// Порядок проверки категорий: чем больше, тем раньше.
    /// </summary>
    [DataField]
    public int Priority;

    /// <summary>
    /// Компоненты, любой из которых относит товар к категории.
    /// </summary>
    [DataField]
    public List<string> Components = new();

    /// <summary>
    /// Теги, любой из которых относит товар к категории.
    /// </summary>
    [DataField]
    public List<ProtoId<TagPrototype>> Tags = new();

    /// <summary>
    /// Сколько кредитов одного товара этой категории рынок недавно скупает по полной цене. Насыщение считается отдельно для каждого товара: каждого газа, каждого материала.
    /// </summary>
    [DataField]
    public double FreeVolume;

    /// <summary>
    /// Объём продаж сверх полной цены в кредитах, при котором товар продаётся вдвое дешевле. Без значения категория не насыщается.
    /// </summary>
    [DataField]
    public double? Capacity;
}
