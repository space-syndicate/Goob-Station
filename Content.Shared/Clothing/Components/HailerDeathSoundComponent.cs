using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.Clothing.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class HailerDeathSoundComponent : Component
{
    [DataField("sound")]
    public SoundSpecifier? Sound;

    
    public bool HasPlayed = false;
}
