using Content.Goobstation.Shared.Xenobiology;
using Robust.Shared.Prototypes;

namespace Content.Goobstation.Shared._Aquila.Xenobiology;

[Prototype]
public sealed partial class SlimeBreedGroupPrototype : IPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// Породы группы, обычно один уровень.
    /// </summary>
    [DataField(required: true)]
    public List<ProtoId<BreedPrototype>> Breeds = new();

    /// <summary>
    /// Перемешиваются ли мутации группы каждый раунд.
    /// </summary>
    [DataField]
    public bool Shuffle = true;

    /// <summary>
    /// Очки исследований за первое сканирование породы из этой группы.
    /// </summary>
    [DataField]
    public int DiscoveryPoints;
}
