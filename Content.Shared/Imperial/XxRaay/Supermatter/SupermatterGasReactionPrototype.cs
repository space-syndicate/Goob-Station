using Content.Shared.Atmos;
using Robust.Shared.Prototypes;

namespace Content.Shared.Imperial.XxRaay.Supermatter;

/// <summary>
/// Прототип реакции газа с суперматерией
/// </summary>
[Prototype("supermatterGasReaction"), DataDefinition]
public sealed partial class SupermatterGasReactionPrototype : IPrototype
{
    [ViewVariables]
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// Газ, при наличии которого должна срабатывать реакция
    /// </summary>
    [DataField(required: true)]
    public Gas Gas { get; private set; }

    /// <summary>
    /// Класс реакции
    /// </summary>
    [DataField(required: true)]
    public Type Reaction { get; private set; } = default!;
}

