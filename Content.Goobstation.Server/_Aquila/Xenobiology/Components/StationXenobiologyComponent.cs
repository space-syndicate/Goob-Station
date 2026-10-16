using Content.Goobstation.Server._Aquila.Xenobiology.Systems;
using Content.Goobstation.Shared.Xenobiology;
using Robust.Shared.Prototypes;

namespace Content.Goobstation.Server._Aquila.Xenobiology.Components;

[RegisterComponent, Access(typeof(SlimeDiscoverySystem), typeof(SlimeMutationShuffleSystem))]
public sealed partial class StationXenobiologyComponent : Component
{
    /// <summary>
    /// Породы, уже отсканированные на станции за раунд.
    /// </summary>
    [DataField]
    public HashSet<ProtoId<BreedPrototype>> Discovered = new();

    /// <summary>
    /// Перемешанный граф мутаций раунда: порода и её возможные мутации.
    /// </summary>
    [DataField]
    public Dictionary<ProtoId<BreedPrototype>, HashSet<ProtoId<BreedPrototype>>> MutationGraph = new();
}
