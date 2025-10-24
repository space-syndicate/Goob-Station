using Robust.Shared.Audio;
using Robust.Shared.Utility;
using Robust.Shared.GameStates;

namespace Content.Shared.Imperial.Lavaland.MiningWeapons.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[AutoGenerateComponentPause]
public sealed partial class SmasherComponent : Component
{
    /// <summary>
    /// When the shield is activated, a beep sounds.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public SoundSpecifier? ActivateSound = new SoundPathSpecifier("/Audio/Weapons/Guns/Gunshots/kinetic_accel.ogg");

    /// <summary>
    /// When the shield is deactivated, a beep sounds.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public SoundSpecifier? DeactivateSound = new SoundPathSpecifier("/Audio/Weapons/Guns/Gunshots/kinetic_accel.ogg");

    /// <summary>
    /// When will the shield end
    /// </summary>
    [AutoNetworkedField]
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    [AutoPausedField]
    public TimeSpan EndTime;

    /// <summary>
    /// Whether this gun is shot via the use key or the alt-use key.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool UseKey = true;

    [DataField]
    public TimeSpan NextActivationTime;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public SpriteSpecifier.Rsi? EffectActived = new(new ResPath("/Textures/Imperial/Lavaland/Entities/Effects/smasher_shield.rsi"), "actived");

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public SpriteSpecifier.Rsi? EffectCharging = new(new ResPath("/Textures/Imperial/Lavaland/Entities/Effects/smasher_shield.rsi"), "charging");

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public SpriteSpecifier.Rsi? EffectDecay = new(new ResPath("/Textures/Imperial/Lavaland/Entities/Effects/smasher_shield.rsi"), "decay");
}
