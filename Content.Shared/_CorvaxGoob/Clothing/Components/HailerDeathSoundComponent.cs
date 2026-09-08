using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared._CorvaxGoob.Clothing.Components;

[RegisterComponent]
public sealed partial class HailerDeathSoundComponent : Component
{
    [DataField]
    public SoundSpecifier? Sound;

    
    public bool HasPlayed = false;
}
