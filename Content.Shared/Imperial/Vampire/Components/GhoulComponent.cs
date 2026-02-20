using Content.Shared.Alert;
using Robust.Shared.Prototypes;
using Robust.Shared.GameStates;
using Content.Shared.StatusIcon;
using Robust.Shared.Audio;

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

    /// <summary>
    /// звук питья крови
    /// </summary>
    [DataField("drinkSound")]
    public SoundSpecifier DrinkSound = new SoundPathSpecifier("/Audio/Items/drink.ogg")
    {
        Params = AudioParams.Default.WithVolume(3)
    };

    [DataField]
    public string MindRoleGhoulID = "MindRoleGhoul";

    /// <summary>
    /// время следующего тика потери крови
    /// </summary>
    public TimeSpan NextBloodDecay = TimeSpan.Zero;

    /// <summary>
    /// количество спрайтов в bleed.rsi. необходимо для расчетов VampireBloodAlert
    /// </summary>
    [DataField]
    public int NumberBloodSections = 10;

    [DataField]
    public string GhoulPuddleID = "VampirePuddle";

    [AutoNetworkedField]
    public float BloodDamage = 0f;

    [AutoNetworkedField]
    public EntityUid Vampire = EntityUid.Invalid;

    [DataField]
    public ProtoId<FactionIconPrototype> StatusIcon = "GhoulFaction";
}
