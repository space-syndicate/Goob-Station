using Content.Goobstation.Server._Aquila.Xenobiology.Systems;
using Content.Shared.EntityEffects;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Goobstation.Server._Aquila.Xenobiology.Components;

[RegisterComponent, AutoGenerateComponentPause, Access(typeof(StabilizedCrossbreedSystem))]
public sealed partial class StabilizedCrossbreedComponent : Component
{
    /// <summary>
    /// Эффекты, периодически применяемые к владельцу.
    /// </summary>
    [DataField(required: true)]
    public EntityEffect[] Effects = default!;

    /// <summary>
    /// Время между применениями эффектов.
    /// </summary>
    [DataField]
    public TimeSpan Interval = TimeSpan.FromSeconds(2);

    /// <summary>
    /// Когда эффекты применятся в следующий раз.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextUpdate;

    /// <summary>
    /// Сколько вложенных контейнеров проверяется в поисках владельца.
    /// </summary>
    [DataField]
    public int MaxHolderDepth = 3;
}
