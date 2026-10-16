using Content.Goobstation.Shared._Aquila.Xenobiology.DataDefinitions;
using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Prototypes;

namespace Content.Goobstation.Shared._Aquila.Xenobiology;

[Prototype]
public sealed partial class SlimeDietPrototype : IPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// Сдвиг генов за единицу каждого съеденного реагента.
    /// </summary>
    [DataField(required: true)]
    public Dictionary<ProtoId<ReagentPrototype>, SlimeGeneShift> Reagents = new();
}
