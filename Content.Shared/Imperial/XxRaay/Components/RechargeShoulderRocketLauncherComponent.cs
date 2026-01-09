using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Imperial.XxRaay.Components;

/// <summary>
/// Компонент для автоперезарядки плечевой ракетной установки.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class RechargeShoulderRocketLauncherComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField]
    [AutoNetworkedField]
    public float RechargeCooldown = 5f;

    [DataField]
    [AutoNetworkedField]
    public SoundSpecifier? RechargeSound = new SoundPathSpecifier("/Audio/Weapons/Guns/MagIn/lmg_magin.ogg")
    {
        Params = AudioParams.Default.WithVolume(-5f)
    };

    [ViewVariables(VVAccess.ReadWrite),
     DataField(customTypeSerializer:typeof(TimeOffsetSerializer)),
    AutoNetworkedField]
    [AutoPausedField]
    public TimeSpan? NextCharge;
}

