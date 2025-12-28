using Robust.Shared.Utility;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Content.Shared.Alert;
using Robust.Shared.Input;

namespace Content.Shared.Imperial.Lavaland.MiningWeapons.Smasher.Components;

[AutoGenerateComponentState]
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentPause]
public sealed partial class SmasherComponent : Component
{
    /// <summary>
    /// There are 6 states in total, each lasting 0.3 seconds.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan TimeDecay = TimeSpan.FromSeconds(1.8f);

    /// <summary>
    /// Includes shield decay time.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan TimeCooldownDownedDecay = TimeSpan.FromSeconds(5.0f);

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float TimeChargingSmasher = 4.0f;

    /// <summary>
    /// Default key on top (not pressed/pressured)
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [AutoNetworkedField]
    public BoundKeyState StateUseKey = BoundKeyState.Up;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public ProtoId<AlertPrototype> CounterCooldownAlert = "SmasherCounterCooldown";

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan TimeCooldownCompleted = TimeSpan.FromSeconds(10f);

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
    public SpriteSpecifier.Rsi EffectActived = new(new ResPath("/Textures/Imperial/Lavaland/MiningWeapons/smasher_shield.rsi"), "actived");

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public SpriteSpecifier.Rsi EffectCharging = new(new ResPath("/Textures/Imperial/Lavaland/MiningWeapons/smasher_shield.rsi"), "charging");

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public SpriteSpecifier.Rsi EffectDecay = new(new ResPath("/Textures/Imperial/Lavaland/MiningWeapons/smasher_shield.rsi"), "decay");
}
