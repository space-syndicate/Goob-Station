using Content.Shared.Inventory;
using Content.Shared.Tag;
using Robust.Shared.Prototypes;

namespace Content.Shared._Aquila.Economy;

[Prototype]
public sealed partial class VendingPriceCategoryPrototype : IPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// Цена товаров этой категории до множителя автомата.
    /// </summary>
    [DataField(required: true)]
    public int Price;

    /// <summary>
    /// Порядок проверки категорий: чем больше, тем раньше.
    /// </summary>
    [DataField]
    public int Priority;

    /// <summary>
    /// Компоненты, которые все должны быть у товара.
    /// </summary>
    [DataField]
    public List<string> Components = new();

    /// <summary>
    /// Теги, хотя бы один из которых должен быть у товара.
    /// </summary>
    [DataField]
    public List<ProtoId<TagPrototype>> Tags = new();

    /// <summary>
    /// Слоты одежды, хотя бы в один из которых должен надеваться товар.
    /// </summary>
    [DataField]
    public SlotFlags Slots = SlotFlags.NONE;
}
