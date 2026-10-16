using Robust.Shared.Prototypes;

namespace Content.Goobstation.Shared._Aquila.Xenobiology;

[Prototype]
public sealed partial class SlimeTraitPrototype : IPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// Название черты в сканере слаймов.
    /// </summary>
    [DataField(required: true)]
    public LocId Name;

    /// <summary>
    /// Описание черты в сканере слаймов.
    /// </summary>
    [DataField(required: true)]
    public LocId Description;

    /// <summary>
    /// Цвет названия черты в сканере слаймов.
    /// </summary>
    [DataField]
    public Color Color = Color.White;

    /// <summary>
    /// Шанс, что новый слайм получит эту черту.
    /// </summary>
    [DataField]
    public float AppearChance = 0.03f;

    /// <summary>
    /// Шанс, что потомок унаследует эту черту.
    /// </summary>
    [DataField]
    public float InheritChance = 0.85f;

    /// <summary>
    /// Черты, несовместимые с этой.
    /// </summary>
    [DataField]
    public List<ProtoId<SlimeTraitPrototype>> Conflicts = new();

    /// <summary>
    /// Экстракты, добавляемые к выходу слайма.
    /// </summary>
    [DataField]
    public int YieldBonus;

    /// <summary>
    /// Множитель шанса мутации слайма.
    /// </summary>
    [DataField]
    public float MutationChanceMultiplier = 1f;

    /// <summary>
    /// Потомки, добавляемые к максимальному числу потомков слайма.
    /// </summary>
    [DataField]
    public int OffspringBonus;

    /// <summary>
    /// Множитель порога сытости для деления.
    /// </summary>
    [DataField]
    public float MitosisHungerMultiplier = 1f;

    /// <summary>
    /// Компоненты, которые слайм получает вместе с чертой.
    /// </summary>
    [DataField]
    public ComponentRegistry Components = new();
}
