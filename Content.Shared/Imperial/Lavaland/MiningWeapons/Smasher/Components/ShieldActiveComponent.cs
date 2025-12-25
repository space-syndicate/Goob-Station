using Robust.Shared.Utility;
using Robust.Shared.GameStates;
using Robust.Shared.Audio;

namespace Content.Shared.Imperial.Lavaland.MiningWeapons.Smasher.Components;

[AutoGenerateComponentState]
[RegisterComponent, NetworkedComponent]
public sealed partial class ShieldActiveComponent : Component
{
    [ViewVariables, AutoNetworkedField]
    public Dictionary<string, float> DamageBlockedCoefficients = new()
    {
        ["Blunt"] = 0.1f,
        ["Slash"] = 0.1f,
        ["Piercing"] = 0.10f,
        ["Heat"] = 0.1f
    };

    [DataField, AutoNetworkedField]
    public SoundSpecifier? DeactivateSound = new SoundPathSpecifier("/Audio/Weapons/Guns/Gunshots/kinetic_accel.ogg");

    [DataField, AutoNetworkedField]
    public SoundSpecifier? ActivateSound = new SoundPathSpecifier("/Audio/Weapons/Guns/Gunshots/kinetic_accel.ogg");

    [DataField, AutoNetworkedField]
    public SpriteSpecifier? EffectActived;

    [DataField, AutoNetworkedField]
    public SpriteSpecifier? EffectCharging;

    [DataField, AutoNetworkedField]
    public SpriteSpecifier? EffectDecay;

    [ViewVariables, AutoNetworkedField]
    public EntityUid SmasherUid;

    [ViewVariables, AutoNetworkedField]
    public EntityUid? UserUid;

    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public TimeSpan EndTime;
}
