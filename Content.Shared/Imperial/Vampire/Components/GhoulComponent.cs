using Content.Shared.Alert;
using Robust.Shared.Prototypes;
using Robust.Shared.GameStates;

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
    public ProtoId<AlertPrototype> BloodAlert = "BloodGhoul";

    /// <summary>
    /// длительность тряски при критическом состоянии
    /// </summary>
    [DataField]
    public TimeSpan ShakingTime = TimeSpan.FromSeconds(5);

    [AutoNetworkedField]
    public float BloodDamage = 0f;

    [AutoNetworkedField]
    public EntityUid? Vampire = null;
}
