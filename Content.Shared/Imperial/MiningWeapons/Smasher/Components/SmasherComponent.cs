using Robust.Shared.Utility;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Content.Shared.Alert;
using Robust.Shared.Input;
using Robust.Shared.Audio;

namespace Content.Shared.Imperial.MiningWeapons.Smasher.Components;

[AutoGenerateComponentState]
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentPause]
public sealed partial class SmasherComponent : Component
{
    [ViewVariables, AutoNetworkedField]
    public Dictionary<string, float> DamageBlockedCoefficients = new()
    {
        ["Blunt"] = 0.4f,
        ["Slash"] = 0.35f,
        ["Piercing"] = 0.3f,
        ["Heat"] = 0.35f
    };

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan ActiveShieldTime = TimeSpan.FromSeconds(15f);

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan TimeDecay = TimeSpan.FromSeconds(1.8f);

    /// <summary>
    /// After this interval, the alert will be deleted if its state is equal to the zero state (0 will be displayed)
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan TimeDeleteAlert = TimeSpan.FromSeconds(3f);

    /// <summary>
    /// When the counter first reached 0
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan? AlertZeroStartTime { get; set; }

    /// <summary>
    /// Cooldown after shield activation
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan ActiveShieldCooldown = TimeSpan.FromSeconds(60f);

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan AlertTime;

    /// <summary>
    /// Includes shield decay time.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan TimeCooldownDownedDecay = TimeSpan.FromSeconds(15f);

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float TimeChargingSmasher = 5.0f;

    /// <summary>
    /// Default key on top (not pressed/pressured)
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [AutoNetworkedField]
    public BoundKeyState StateUseKey = BoundKeyState.Up;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public ProtoId<AlertPrototype> CounterCooldownAlert = "SmasherCounterCooldown";

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
    public SpriteSpecifier.Rsi EffectActived = new(new ResPath("/Textures/Imperial/MiningWeapons/Smasher/smasher_shield.rsi"), "actived");

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public SpriteSpecifier.Rsi EffectCharging = new(new ResPath("/Textures/Imperial/MiningWeapons/Smasher/smasher_shield.rsi"), "charging");

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public SpriteSpecifier.Rsi EffectDecay = new(new ResPath("/Textures/Imperial/MiningWeapons/Smasher/smasher_shield.rsi"), "decay");

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public SoundSpecifier? StartChargingSound = new SoundPathSpecifier("/Audio/Imperial/MiningWeapons/Smasher/start_charging_shield.ogg");

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public SoundSpecifier? DeactivateSound = new SoundPathSpecifier("/Audio/Imperial/MiningWeapons/Smasher/deactivate_shield.ogg");

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public SoundSpecifier? ActivateSound = new SoundPathSpecifier("/Audio/Imperial/MiningWeapons/Smasher/activate_shield.ogg");
}
