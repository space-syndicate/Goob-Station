namespace Content.Goobstation.Shared._Aquila.Xenobiology.DataDefinitions;

[DataDefinition]
public sealed partial class SlimeGeneShift
{
    /// <summary>
    /// Изменение шанса мутации за единицу реагента.
    /// </summary>
    [DataField]
    public float MutationChance;

    /// <summary>
    /// Изменение порога деления за единицу реагента.
    /// </summary>
    [DataField]
    public float MitosisHunger;

    /// <summary>
    /// Шанс за единицу реагента увеличить максимальное число потомков на одного.
    /// </summary>
    [DataField]
    public float OffspringChance;

    /// <summary>
    /// Шанс за единицу реагента увеличить число экстрактов на один.
    /// </summary>
    [DataField]
    public float YieldChance;
}
