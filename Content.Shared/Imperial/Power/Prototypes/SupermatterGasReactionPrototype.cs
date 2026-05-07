using Content.Shared.Atmos;
using Content.Shared.Imperial.Power.Components;
using Content.Shared.Imperial.Power.GasReactions;
using Robust.Shared.Prototypes;

namespace Content.Shared.Imperial.Power.Prototypes;

/// <summary>
/// Describes how supermatter reacts to a specific gas.
/// </summary>
[Prototype("supermatterGasReaction")]
public sealed partial class SupermatterGasReactionPrototype : IPrototype
{
    [ViewVariables]
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// Gas prototype ID / Gas enum name. Parsed via <see cref="AtmosCommandUtils.TryParseGasID"/>.
    /// </summary>
    [DataField("gas", required: true)]
    public string Gas { get; private set; } = default!;

    /// <summary>
    /// Reaction logic implementation.
    /// </summary>
    [DataField("reaction", required: true)]
    public ISupermatterGasReaction Reaction { get; private set; } = default!;

    /// <summary>
    /// Higher runs earlier. Useful for shutdown reactions.
    /// </summary>
    [DataField("priority")]
    public int Priority { get; private set; }

    /// <summary>
    /// Whether this reaction should run even if the supermatter is not activated.
    /// </summary>
    [DataField("appliesWhenInactive")]
    public bool AppliesWhenInactive { get; private set; }

    /// <summary>
    /// Whether the gas should still be consumed while the supermatter is inactive.
    /// </summary>
    [DataField("consumeWhenInactive")]
    public bool ConsumeWhenInactive { get; private set; }

    /// <summary>
    /// Whether to call <see cref="ISupermatterGasReaction.React"/> even when the activation threshold is not met.
    /// </summary>
    [DataField("processWhenBelowThreshold")]
    public bool ProcessWhenBelowThreshold { get; private set; }
}

