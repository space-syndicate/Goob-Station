using Content.Goobstation.Shared._Aquila.Xenobiology.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Goobstation.Shared._Aquila.Xenobiology.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, Access(typeof(SlimeCrossbreedingSystem))]
public sealed partial class SlimeCrossbreedingComponent : Component
{
    /// <summary>
    /// Экстракт, которым кормят слайма.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntProtoId? Extract;

    /// <summary>
    /// Сколько экстрактов слайм уже поглотил.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int Fed;
}
