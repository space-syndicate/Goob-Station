using Content.Shared.Alert;
using Robust.Shared.Prototypes;
using Robust.Shared.GameStates;
using Content.Shared.StatusIcon;

namespace Content.Shared.Imperial.Vampire;

[RegisterComponent, AutoGenerateComponentState, NetworkedComponent]
public sealed partial class GhoulComponent : Component
{
    /// <summary>
    /// стартовое количество крови у упыря
    /// </summary>
    [AutoNetworkedField]
    public float CritThreshold = 100f;

    /// <summary>
    /// Интервал между тиками потери крови
    /// </summary>
    [DataField]
    public TimeSpan BloodDecayInterval = TimeSpan.FromSeconds(30);

    /// <summary>
    /// количество урона за каждый тик
    /// </summary>
    [DataField]
    public float BloodDecayAmount = 2f;

    /// <summary>
    /// время следующего тика потери крови
    /// </summary>
    [DataField]
    public TimeSpan NextBloodDecay = TimeSpan.Zero;

    /// <summary>
    /// ID алерта для отображения уровня крови
    /// </summary>
    [DataField]
    public ProtoId<AlertPrototype> BloodAlert = "VampireBloodAlert";

    /// <summary>
    /// длительность тряски при критическом состоянии
    /// </summary>
    [DataField]
    public TimeSpan ShakingTime = TimeSpan.FromSeconds(5);

    /// <summary>
    /// количество выпитой крови за 1 тик
    /// </summary>
    [DataField("bloodPerTick")]
    public float BloodPerTick = 1;

    /// <summary>
    /// сколько занимает излечение упыря
    /// </summary>
    [DataField("ghoulCure")]
    public TimeSpan GhoulCure = TimeSpan.FromSeconds(15);

    [AutoNetworkedField]
    public float BloodDamage = 0f;

    [AutoNetworkedField]
    public EntityUid Vampire;

    [DataField]
    public ProtoId<FactionIconPrototype> StatusIcon = "GhoulFaction";
}
