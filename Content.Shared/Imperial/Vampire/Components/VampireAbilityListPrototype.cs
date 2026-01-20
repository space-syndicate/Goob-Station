using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Imperial.Vampire;

[Prototype("vampireAbilityList")]
public sealed class VampireAbilityListPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// список выдаваемых способностей
    /// </summary>
    [DataField("abilities", required: true)]
    public List<EntProtoId> Abilities = new();

    /// <summary>
    /// пороги крови для получения способностей
    /// </summary>
    [DataField("thresholds")]
    public Dictionary<int, int> Thresholds = new();

    /// <summary>
    /// улучшенные версии способностей
    /// </summary>
    [DataField("upgrades")]
    public List<EntProtoId> Upgrades = new();
}
