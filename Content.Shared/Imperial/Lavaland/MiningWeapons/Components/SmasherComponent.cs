using Robust.Shared.Utility;
using Robust.Shared.GameStates;

namespace Content.Shared.Imperial.Lavaland.MiningWeapons.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[AutoGenerateComponentPause]
public sealed partial class SmasherComponent : Component
{
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
    public SpriteSpecifier.Rsi? EffectActived = new(new ResPath("/Textures/Imperial/Lavaland/MiningWeapons/smasher_shield.rsi"), "actived");

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public SpriteSpecifier.Rsi? EffectCharging = new(new ResPath("/Textures/Imperial/Lavaland/MiningWeapons/smasher_shield.rsi"), "charging");

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public SpriteSpecifier.Rsi? EffectDecay = new(new ResPath("/Textures/Imperial/Lavaland/MiningWeapons/smasher_shield.rsi"), "decay");
}
