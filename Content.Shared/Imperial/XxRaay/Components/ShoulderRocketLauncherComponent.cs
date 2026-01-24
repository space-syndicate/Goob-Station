using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Imperial.XxRaay.Components;

/// <summary>
/// Компонент для плечевой ракетной установки с системой захвата целей.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(raiseAfterAutoHandleState: true)]
public sealed partial class ShoulderRocketLauncherComponent : Component
{
    /// <summary>
    /// Текущее количество зарядов.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int Charges = 6;

    /// <summary>
    /// Максимальное количество зарядов.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int MaxCharges = 6;

    [DataField, AutoNetworkedField]
    public bool IsTargeting = false;

    /// <summary>
    /// Прототип ракеты.
    /// </summary>
    [DataField]
    public EntProtoId ProjectilePrototype = "BulletShoulderRocket";

    /// <summary>
    /// Максимальное количество целей для захвата.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int MaxTargetCount = 6;
}

