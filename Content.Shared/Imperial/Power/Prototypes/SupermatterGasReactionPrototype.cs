using System.Collections.Generic;
using Content.Shared.Atmos;
using Content.Shared.Imperial.Power.Components;
using Content.Shared.Imperial.Power.GasReactions;
using Robust.Shared.Prototypes;

namespace Content.Shared.Imperial.Power.Prototypes;

/// <summary>
/// Describes how supermatter reacts to a specific gas.
/// </summary>
[Prototype]
public sealed partial class SupermatterGasReactionPrototype : IPrototype
{
    [ViewVariables]
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// Gas prototype ID / Gas enum name. Parsed via <see cref="AtmosCommandUtils.TryParseGasID"/>.
    /// </summary>
    [DataField(required: true)]
    public string Gas { get; private set; } = default!;

    /// <summary>
    /// Reaction logic implementations.
    /// </summary>
    [DataField(required: true)]
    public List<ISupermatterGasReaction> Reactions { get; private set; } = new();

    /// <summary>
    /// Higher runs earlier. Useful for shutdown reactions.
    /// </summary>
    [DataField]
    public int Priority { get; private set; }

    /// <summary>
    /// Whether this reaction should run even if the supermatter is not activated.
    /// </summary>
    [DataField]
    public bool AppliesWhenInactive { get; private set; }

    /// <summary>
    /// Whether the gas should still be consumed while the supermatter is inactive.
    /// </summary>
    [DataField]
    public bool ConsumeWhenInactive { get; private set; }

    /// <summary>
    /// Whether to call <see cref="ISupermatterGasReaction.React"/> even when the activation threshold is not met.
    /// </summary>
    [DataField]
    public bool ProcessWhenBelowThreshold { get; private set; }
}

