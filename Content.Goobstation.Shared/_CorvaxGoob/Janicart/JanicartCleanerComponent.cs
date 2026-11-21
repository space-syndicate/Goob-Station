using Content.Goobstation.Maths.FixedPoint;
using Content.Shared.Chemistry.Reagent;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Goobstation.Shared._CorvaxGoob.Janicart;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(JanicartCleanerSystem))]
public sealed partial class JanicartCleanerComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool Enabled;

    /// <summary>
    ///     Реагенты из бака, которые льются на пол.
    /// </summary>
    [DataField]
    public ProtoId<ReagentPrototype>[] Reagents = { "SpaceCleaner", "Water" };

    /// <summary>
    ///     Максимальный расход за тайл, неиспользованное возвращается в бак.
    /// </summary>
    [DataField]
    public FixedPoint2 DosePerTile = 10;

    /// <summary>
    ///     Сколько воды уходит на одну декаль.
    /// </summary>
    [DataField]
    public FixedPoint2 WaterPerDecal = 3;

    [DataField, AutoNetworkedField]
    public EntityUid? ToggleActionEntity;
}
