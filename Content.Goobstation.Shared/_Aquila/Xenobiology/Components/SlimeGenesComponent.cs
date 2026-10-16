using Content.Goobstation.Shared._Aquila.Xenobiology.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Goobstation.Shared._Aquila.Xenobiology.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, Access(typeof(SlimeGeneticsSystem))]
public sealed partial class SlimeGenesComponent : Component
{
    /// <summary>
    /// Черты слайма.
    /// </summary>
    [DataField, AutoNetworkedField]
    public HashSet<ProtoId<SlimeTraitPrototype>> Traits = new();

    /// <summary>
    /// Какие реагенты при поедании сдвигают гены слайма.
    /// </summary>
    [DataField]
    public ProtoId<SlimeDietPrototype> Diet = "SlimeDietDefault";

    /// <summary>
    /// Максимальное случайное изменение шанса мутации при смене поколения.
    /// </summary>
    [DataField]
    public float MutationChanceDrift = 0.05f;

    /// <summary>
    /// Минимальный шанс мутации по генам.
    /// </summary>
    [DataField]
    public float MinMutationChance;

    /// <summary>
    /// Максимальный шанс мутации по генам.
    /// </summary>
    [DataField]
    public float MaxMutationChance = 0.9f;

    /// <summary>
    /// Шанс, что число потомков изменится на одного при смене поколения.
    /// </summary>
    [DataField]
    public float OffspringDriftChance = 0.2f;

    /// <summary>
    /// Минимальное число потомков по генам.
    /// </summary>
    [DataField]
    public int MinOffspring = 1;

    /// <summary>
    /// Максимальное число потомков по генам.
    /// </summary>
    [DataField]
    public int MaxOffspring = 6;

    /// <summary>
    /// Шанс, что число экстрактов изменится на один при смене поколения.
    /// </summary>
    [DataField]
    public float YieldDriftChance = 0.15f;

    /// <summary>
    /// Минимальное число экстрактов по генам.
    /// </summary>
    [DataField]
    public int MinYield = 1;

    /// <summary>
    /// Максимальное число экстрактов по генам.
    /// </summary>
    [DataField]
    public int MaxYield = 6;

    /// <summary>
    /// Максимальное относительное изменение порога деления при смене поколения.
    /// </summary>
    [DataField]
    public float MitosisHungerDrift = 0.06f;

    /// <summary>
    /// Минимальный порог сытости для деления по генам.
    /// </summary>
    [DataField]
    public float MinMitosisHunger = 90f;

    /// <summary>
    /// Максимальный порог сытости для деления по генам.
    /// </summary>
    [DataField]
    public float MaxMitosisHunger = 145f;
}
