using Robust.Shared.Audio;
using Robust.Shared.Serialization;

namespace Content.Goobstation.Shared.Radio;

[Serializable, NetSerializable]
public sealed class PlayRadioBarkEvent : EntityEventArgs
{
    public SoundSpecifier Sound = default!;
}
