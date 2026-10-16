using Content.Goobstation.Server._Aquila.Xenobiology.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Goobstation.Server._Aquila.Xenobiology.Components;

[RegisterComponent, AutoGenerateComponentPause, Access(typeof(IndustrialCrossbreedSystem))]
public sealed partial class IndustrialCrossbreedComponent : Component
{
    /// <summary>
    /// Сущности, производимые каждый цикл.
    /// </summary>
    [DataField(required: true)]
    public List<EntProtoId> Spawns = new();

    /// <summary>
    /// Время между циклами производства.
    /// </summary>
    [DataField]
    public TimeSpan Interval = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Сколько циклов производства осталось до истощения гибрида.
    /// </summary>
    [DataField]
    public int Cycles = 20;

    /// <summary>
    /// Когда наступит следующий цикл производства.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextSpawn;
}
