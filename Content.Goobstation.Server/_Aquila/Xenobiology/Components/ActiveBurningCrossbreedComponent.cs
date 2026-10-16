using Content.Goobstation.Server._Aquila.Xenobiology.Systems;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Goobstation.Server._Aquila.Xenobiology.Components;

[RegisterComponent, AutoGenerateComponentPause, Access(typeof(BurningCrossbreedSystem))]
public sealed partial class ActiveBurningCrossbreedComponent : Component
{
    /// <summary>
    /// Когда гибрид сработает.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan ActivateAt;

    /// <summary>
    /// Кто активировал гибрид.
    /// </summary>
    [DataField]
    public EntityUid? User;
}
